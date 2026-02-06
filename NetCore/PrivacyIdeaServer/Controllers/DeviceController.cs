// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Mvc;
using TwoFactorAuth.Core.Models;
using TwoFactorAuth.Core.Services;

namespace PrivacyIdeaServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceManagementService _deviceService;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(
        IDeviceManagementService deviceService,
        ILogger<DeviceController> logger)
    {
        _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Request device activation - generates OTP code to display on web interface
    /// </summary>
    [HttpPost("request-activation")]
    [ProducesResponseType(typeof(ActivationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ActivationResponse>> RequestActivation(
        [FromBody] ActivationRequest request)
    {
        if (string.IsNullOrEmpty(request.DeviceId))
        {
            return BadRequest(new { message = "Device ID is required" });
        }

        _logger.LogInformation("Device activation requested for {DeviceId}", request.DeviceId);

        var result = await _deviceService.RequestDeviceActivationAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Activate device with OTP code entered by user
    /// </summary>
    [HttpPost("activate")]
    [ProducesResponseType(typeof(ActivationValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ActivationValidationResult>> ActivateDevice(
        [FromBody] DeviceActivationValidation validation)
    {
        if (string.IsNullOrEmpty(validation.DeviceId) ||
            string.IsNullOrEmpty(validation.OtpCode))
        {
            return BadRequest(new { message = "Device ID and OTP code are required" });
        }

        _logger.LogInformation("Activating device {DeviceId}", validation.DeviceId);

        var result = await _deviceService.ValidateDeviceActivationAsync(validation);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Check if device is activated
    /// </summary>
    [HttpGet("{deviceId}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetDeviceStatus(string deviceId)
    {
        var isActivated = await _deviceService.IsDeviceActivatedAsync(deviceId);
        var deviceInfo = await _deviceService.GetDeviceInfoAsync(deviceId);

        return Ok(new
        {
            deviceId,
            isActivated,
            deviceInfo
        });
    }

    /// <summary>
    /// Get all activated devices
    /// </summary>
    [HttpGet("activated")]
    [ProducesResponseType(typeof(IEnumerable<DeviceInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DeviceInfo>>> GetActivatedDevices()
    {
        var devices = await _deviceService.GetActivatedDevicesAsync();
        return Ok(devices);
    }

    /// <summary>
    /// Deactivate a device
    /// </summary>
    [HttpDelete("{deviceId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeactivateDevice(string deviceId)
    {
        _logger.LogInformation("Deactivating device {DeviceId}", deviceId);

        var result = await _deviceService.DeactivateDeviceAsync(deviceId);

        if (result)
        {
            return Ok(new { message = "Device deactivated successfully" });
        }

        return NotFound(new { message = "Device not found" });
    }
}
