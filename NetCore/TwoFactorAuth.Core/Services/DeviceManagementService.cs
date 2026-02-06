// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TwoFactorAuth.Core.Models;
using OtpNet;

namespace TwoFactorAuth.Core.Services;

/// <summary>
/// Interface for device management operations
/// </summary>
public interface IDeviceManagementService
{
    /// <summary>
    /// Request device activation and generate OTP for display
    /// </summary>
    Task<ActivationResponse> RequestDeviceActivationAsync(ActivationRequest request);
    
    /// <summary>
    /// Validate device activation with OTP code
    /// </summary>
    Task<ActivationValidationResult> ValidateDeviceActivationAsync(DeviceActivationValidation validation);
    
    /// <summary>
    /// Check if device is whitelisted and activated
    /// </summary>
    Task<bool> IsDeviceActivatedAsync(string deviceId);
    
    /// <summary>
    /// Get device information
    /// </summary>
    Task<DeviceInfo?> GetDeviceInfoAsync(string deviceId);
    
    /// <summary>
    /// Get all activated devices
    /// </summary>
    Task<IEnumerable<DeviceInfo>> GetActivatedDevicesAsync();
    
    /// <summary>
    /// Deactivate a device
    /// </summary>
    Task<bool> DeactivateDeviceAsync(string deviceId);
}

/// <summary>
/// Device management service implementation
/// </summary>
public class DeviceManagementService : IDeviceManagementService
{
    private readonly ILogger<DeviceManagementService> _logger;
    
    // In-memory storage for demonstration
    // In production, use a database
    private readonly ConcurrentDictionary<string, DeviceInfo> _devices = new();
    private readonly ConcurrentDictionary<string, (string Secret, DateTime ExpiresAt)> _pendingActivations = new(); // deviceId -> (secret, expiry)
    
    // Activation requests expire after 5 minutes
    private const int ActivationExpiryMinutes = 5;
    
    public DeviceManagementService(ILogger<DeviceManagementService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public Task<ActivationResponse> RequestDeviceActivationAsync(ActivationRequest request)
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
            _pendingActivations[request.DeviceId] = (secret, expiresAt);
            
            // Store device info as pending
            var deviceInfo = new DeviceInfo
            {
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName ?? "Unknown Device",
                Platform = request.Platform ?? "Unknown",
                OsVersion = request.OsVersion ?? "Unknown",
                Model = request.Model ?? "Unknown",
                RegisteredAt = DateTime.UtcNow,
                IsActivated = false
            };
            
            _devices.AddOrUpdate(request.DeviceId, deviceInfo, (key, existing) => deviceInfo);
            
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
            
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting device activation for {DeviceId}", request.DeviceId);
            return Task.FromResult(new ActivationResponse
            {
                Success = false,
                Message = $"Error requesting activation: {ex.Message}"
            });
        }
    }
    
    public Task<ActivationValidationResult> ValidateDeviceActivationAsync(DeviceActivationValidation validation)
    {
        try
        {
            _logger.LogInformation("Validating device activation for {DeviceId}", validation.DeviceId);
            
            // Check if we have a pending activation for this device
            if (!_pendingActivations.TryGetValue(validation.DeviceId, out var activationData))
            {
                _logger.LogWarning("No pending activation found for device {DeviceId}", validation.DeviceId);
                return Task.FromResult(new ActivationValidationResult
                {
                    Success = false,
                    Message = "No pending activation found for this device. Please request activation first."
                });
            }
            
            // Check if activation has expired
            if (DateTime.UtcNow > activationData.ExpiresAt)
            {
                _pendingActivations.TryRemove(validation.DeviceId, out _);
                _logger.LogWarning("Activation request expired for device {DeviceId}", validation.DeviceId);
                return Task.FromResult(new ActivationValidationResult
                {
                    Success = false,
                    Message = "Activation request has expired. Please request activation again."
                });
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
                return Task.FromResult(new ActivationValidationResult
                {
                    Success = false,
                    Message = "Invalid OTP code. Please try again."
                });
            }
            
            // Mark device as activated
            if (_devices.TryGetValue(validation.DeviceId, out var deviceInfo))
            {
                deviceInfo.IsActivated = true;
                deviceInfo.ActivatedAt = DateTime.UtcNow;
                deviceInfo.ActivationToken = Guid.NewGuid().ToString();
                
                // Remove from pending activations
                _pendingActivations.TryRemove(validation.DeviceId, out _);
                
                _logger.LogInformation("Device {DeviceId} activated successfully", validation.DeviceId);
                
                return Task.FromResult(new ActivationValidationResult
                {
                    Success = true,
                    Message = "Device activated successfully!",
                    ActivationToken = deviceInfo.ActivationToken,
                    ActivatedAt = deviceInfo.ActivatedAt
                });
            }
            else
            {
                _logger.LogError("Device info not found for {DeviceId}", validation.DeviceId);
                return Task.FromResult(new ActivationValidationResult
                {
                    Success = false,
                    Message = "Device information not found."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating device activation for {DeviceId}", validation.DeviceId);
            return Task.FromResult(new ActivationValidationResult
            {
                Success = false,
                Message = $"Error validating activation: {ex.Message}"
            });
        }
    }
    
    public Task<bool> IsDeviceActivatedAsync(string deviceId)
    {
        if (_devices.TryGetValue(deviceId, out var deviceInfo))
        {
            return Task.FromResult(deviceInfo.IsActivated);
        }
        return Task.FromResult(false);
    }
    
    public Task<DeviceInfo?> GetDeviceInfoAsync(string deviceId)
    {
        _devices.TryGetValue(deviceId, out var deviceInfo);
        return Task.FromResult(deviceInfo);
    }
    
    public Task<IEnumerable<DeviceInfo>> GetActivatedDevicesAsync()
    {
        var activatedDevices = _devices.Values.Where(d => d.IsActivated).ToList();
        return Task.FromResult<IEnumerable<DeviceInfo>>(activatedDevices);
    }
    
    public Task<bool> DeactivateDeviceAsync(string deviceId)
    {
        if (_devices.TryGetValue(deviceId, out var deviceInfo))
        {
            deviceInfo.IsActivated = false;
            deviceInfo.ActivatedAt = null;
            deviceInfo.ActivationToken = null;
            
            _logger.LogInformation("Device {DeviceId} deactivated", deviceId);
            return Task.FromResult(true);
        }
        
        _logger.LogWarning("Device {DeviceId} not found for deactivation", deviceId);
        return Task.FromResult(false);
    }
}
