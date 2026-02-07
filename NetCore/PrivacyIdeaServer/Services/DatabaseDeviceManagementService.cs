// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TwoFactorAuth.Core.Models;
using TwoFactorAuth.Core.Services;
using OtpNet;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Services;

/// <summary>
/// Database-backed device management service implementation
/// Persists device information to database for recovery scenarios
/// </summary>
public class DatabaseDeviceManagementService : IDeviceManagementService
{
    private readonly ILogger<DatabaseDeviceManagementService> _logger;
    private readonly PrivacyIDEAContext _context;
    
    // Activation requests expire after 5 minutes
    private const int ActivationExpiryMinutes = 5;
    
    public DatabaseDeviceManagementService(
        ILogger<DatabaseDeviceManagementService> logger,
        PrivacyIDEAContext context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    
    public async Task<ActivationResponse> RequestDeviceActivationAsync(ActivationRequest request)
    {
        try
        {
            _logger.LogInformation("Device activation requested for {DeviceId}", request.DeviceId);
            
            // Generate secret for this device
            var secretKey = KeyGeneration.GenerateRandomKey(20);
            var secret = Base32Encoding.ToString(secretKey);
            
            // Generate current OTP code to display
            var totp = new Totp(secretKey, step: 30, totpSize: 6);
            var otpCode = totp.ComputeTotp();
            
            // Store pending activation with expiration
            var expiresAt = DateTime.UtcNow.AddMinutes(ActivationExpiryMinutes);
            
            // Remove any existing pending activation for this device
            var existingPending = await _context.PendingDeviceActivations
                .FirstOrDefaultAsync(p => p.DeviceId == request.DeviceId);
            
            if (existingPending != null)
            {
                _context.PendingDeviceActivations.Remove(existingPending);
            }
            
            // Create new pending activation
            var pendingActivation = new PendingDeviceActivation(
                request.DeviceId,
                secret,
                expiresAt
            );
            
            await _context.PendingDeviceActivations.AddAsync(pendingActivation);
            
            // Store or update device info as pending
            var existingDevice = await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId);
            
            if (existingDevice != null)
            {
                // Update existing device
                existingDevice.DeviceName = request.DeviceName ?? "Unknown Device";
                existingDevice.Platform = request.Platform ?? "Unknown";
                existingDevice.OsVersion = request.OsVersion ?? "Unknown";
                existingDevice.Model = request.Model ?? "Unknown";
                existingDevice.RegisteredAt = DateTime.UtcNow;
            }
            else
            {
                // Create new device
                var device = new Device(
                    request.DeviceId,
                    request.DeviceName ?? "Unknown Device",
                    request.Platform ?? "Unknown",
                    request.OsVersion ?? "Unknown",
                    request.Model ?? "Unknown"
                );
                
                await _context.Devices.AddAsync(device);
            }
            
            await _context.SaveChangesAsync();
            
            var response = new ActivationResponse
            {
                Success = true,
                Message = "Activation OTP generated. Please enter this code on your device.",
                Secret = secret,
                OtpCode = otpCode,
                DeviceId = request.DeviceId
            };
            
            _logger.LogInformation("Generated activation OTP for device {DeviceId}: {OtpCode}", 
                request.DeviceId, otpCode);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting device activation for {DeviceId}", request.DeviceId);
            return new ActivationResponse
            {
                Success = false,
                Message = $"Error requesting activation: {ex.Message}"
            };
        }
    }
    
    public async Task<ActivationValidationResult> ValidateDeviceActivationAsync(DeviceActivationValidation validation)
    {
        try
        {
            _logger.LogInformation("Validating device activation for {DeviceId}", validation.DeviceId);
            
            // Check if we have a pending activation for this device
            var activationData = await _context.PendingDeviceActivations
                .FirstOrDefaultAsync(p => p.DeviceId == validation.DeviceId);
            
            if (activationData == null)
            {
                _logger.LogWarning("No pending activation found for device {DeviceId}", validation.DeviceId);
                return new ActivationValidationResult
                {
                    Success = false,
                    Message = "No pending activation found for this device. Please request activation first."
                };
            }
            
            // Check if activation has expired
            if (DateTime.UtcNow > activationData.ExpiresAt)
            {
                _context.PendingDeviceActivations.Remove(activationData);
                await _context.SaveChangesAsync();
                
                _logger.LogWarning("Activation request expired for device {DeviceId}", validation.DeviceId);
                return new ActivationValidationResult
                {
                    Success = false,
                    Message = "Activation request has expired. Please request activation again."
                };
            }
            
            var secret = activationData.Secret;
            
            // Validate OTP code
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes, step: 30, totpSize: 6);
            
            long timeStepMatched;
            var isValid = totp.VerifyTotp(validation.OtpCode, out timeStepMatched, 
                new VerificationWindow(1, 1)); // Allow 1 step before and after
            
            if (!isValid)
            {
                _logger.LogWarning("Invalid OTP code for device {DeviceId}", validation.DeviceId);
                return new ActivationValidationResult
                {
                    Success = false,
                    Message = "Invalid OTP code. Please try again."
                };
            }
            
            // Mark device as activated
            var deviceInfo = await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == validation.DeviceId);
            
            if (deviceInfo != null)
            {
                deviceInfo.IsActivated = true;
                deviceInfo.ActivatedAt = DateTime.UtcNow;
                deviceInfo.ActivationToken = Guid.NewGuid().ToString();
                
                // Remove from pending activations
                _context.PendingDeviceActivations.Remove(activationData);
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Device {DeviceId} activated successfully", validation.DeviceId);
                
                return new ActivationValidationResult
                {
                    Success = true,
                    Message = "Device activated successfully!",
                    ActivationToken = deviceInfo.ActivationToken,
                    ActivatedAt = deviceInfo.ActivatedAt
                };
            }
            else
            {
                _logger.LogError("Device info not found for {DeviceId}", validation.DeviceId);
                return new ActivationValidationResult
                {
                    Success = false,
                    Message = "Device information not found."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating device activation for {DeviceId}", validation.DeviceId);
            return new ActivationValidationResult
            {
                Success = false,
                Message = $"Error validating activation: {ex.Message}"
            };
        }
    }
    
    public async Task<bool> IsDeviceActivatedAsync(string deviceId)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);
        
        return device?.IsActivated ?? false;
    }
    
    public async Task<DeviceInfo?> GetDeviceInfoAsync(string deviceId)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);
        
        if (device == null)
        {
            return null;
        }
        
        return MapToDeviceInfo(device);
    }
    
    public async Task<IEnumerable<DeviceInfo>> GetActivatedDevicesAsync()
    {
        var devices = await _context.Devices
            .Where(d => d.IsActivated)
            .ToListAsync();
        
        return devices.Select(MapToDeviceInfo);
    }
    
    public async Task<bool> DeactivateDeviceAsync(string deviceId)
    {
        var deviceInfo = await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);
        
        if (deviceInfo != null)
        {
            deviceInfo.IsActivated = false;
            deviceInfo.ActivatedAt = null;
            deviceInfo.ActivationToken = null;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Device {DeviceId} deactivated", deviceId);
            return true;
        }
        
        _logger.LogWarning("Device {DeviceId} not found for deactivation", deviceId);
        return false;
    }
    
    /// <summary>
    /// Clean up expired pending activations
    /// Should be called periodically (e.g., via background service)
    /// </summary>
    public async Task CleanupExpiredActivationsAsync()
    {
        try
        {
            var expiredActivations = await _context.PendingDeviceActivations
                .Where(p => p.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();
            
            if (expiredActivations.Any())
            {
                _context.PendingDeviceActivations.RemoveRange(expiredActivations);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cleaned up {Count} expired pending activations", 
                    expiredActivations.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired activations");
        }
    }
    
    private static DeviceInfo MapToDeviceInfo(Device device)
    {
        return new DeviceInfo
        {
            DeviceId = device.DeviceId,
            DeviceName = device.DeviceName,
            Platform = device.Platform,
            OsVersion = device.OsVersion,
            Model = device.Model,
            RegisteredAt = device.RegisteredAt,
            IsActivated = device.IsActivated,
            ActivatedAt = device.ActivatedAt,
            ActivationToken = device.ActivationToken
        };
    }
}
