# TwoFactorAuth.Core

A .NET class library for implementing two-factor authentication with device management. This package provides reusable OTP generation, validation, and device activation services for Blazor and ASP.NET Core applications.

## Features

- 🔐 **Device Management**: Manage and whitelist authorized devices
- 🔑 **OTP Generation**: Generate TOTP codes for device activation
- ✅ **OTP Validation**: Validate OTP codes with time window support
- 🚀 **Device Activation Flow**: Complete OTP-based device activation
- 💾 **In-Memory Storage**: Built-in in-memory storage (replaceable with database)
- 🌐 **ASP.NET Core Integration**: Easy integration with web applications

## Installation

Add the package reference to your project:

```xml
<ItemGroup>
  <ProjectReference Include="..\TwoFactorAuth.Core\TwoFactorAuth.Core.csproj" />
</ItemGroup>
```

Or if published as a NuGet package:

```bash
dotnet add package TwoFactorAuth.Core
```

## Dependencies

- `Otp.NET`: OTP generation and validation
- `Microsoft.Extensions.Logging.Abstractions`: Logging support

## Usage

### 1. Service Registration

Register the device management service in your ASP.NET Core application:

```csharp
using TwoFactorAuth.Core.Services;

// In Program.cs or Startup.cs
builder.Services.AddSingleton<IDeviceManagementService, DeviceManagementService>();
```

### 2. Device Activation Flow

#### Step 1: Request Activation (Server-side)

When a user wants to activate a device, the server generates an OTP code:

```csharp
using TwoFactorAuth.Core.Models;
using TwoFactorAuth.Core.Services;

[ApiController]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceManagementService _deviceService;
    
    public DeviceController(IDeviceManagementService deviceService)
    {
        _deviceService = deviceService;
    }
    
    [HttpPost("request-activation")]
    public async Task<IActionResult> RequestActivation([FromBody] ActivationRequest request)
    {
        var result = await _deviceService.RequestDeviceActivationAsync(request);
        
        if (result.Success)
        {
            // Display OTP code to admin/web interface
            // The mobile device will enter this code to activate
            return Ok(result);
        }
        
        return BadRequest(result);
    }
}
```

#### Step 2: Validate Activation (Server-side)

When the device enters the OTP code, validate it:

```csharp
[HttpPost("activate")]
public async Task<IActionResult> ActivateDevice([FromBody] DeviceActivationValidation validation)
{
    var result = await _deviceService.ValidateDeviceActivationAsync(validation);
    
    if (result.Success)
    {
        return Ok(result);
    }
    
    return BadRequest(result);
}
```

### 3. Blazor Component Example

Create a Blazor component to display OTP codes for device activation:

```razor
@page "/device-activation"
@inject IDeviceManagementService DeviceService

<h3>Device Activation</h3>

@if (!string.IsNullOrEmpty(otpCode))
{
    <div class="alert alert-info">
        <h4>Activation Code</h4>
        <p class="display-4 text-center">@otpCode</p>
        <p class="text-center">
            <small>Enter this code on your mobile device</small>
        </p>
    </div>
    
    <div class="alert alert-warning">
        <strong>Device ID:</strong> @deviceId
    </div>
}
else
{
    <div class="mb-3">
        <label>Device ID</label>
        <input @bind="deviceId" class="form-control" />
    </div>
    
    <div class="mb-3">
        <label>Username</label>
        <input @bind="username" class="form-control" />
    </div>
    
    <button @onclick="GenerateActivationCode" class="btn btn-primary">
        Generate Activation Code
    </button>
}

<h4 class="mt-4">Activated Devices</h4>
<table class="table">
    <thead>
        <tr>
            <th>Device ID</th>
            <th>Name</th>
            <th>Platform</th>
            <th>Activated At</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var device in activatedDevices)
        {
            <tr>
                <td>@device.DeviceId</td>
                <td>@device.DeviceName</td>
                <td>@device.Platform @device.OsVersion</td>
                <td>@device.ActivatedAt?.ToLocalTime()</td>
                <td>
                    <button @onclick="() => DeactivateDevice(device.DeviceId)" 
                            class="btn btn-sm btn-danger">
                        Deactivate
                    </button>
                </td>
            </tr>
        }
    </tbody>
</table>

@code {
    private string deviceId = "";
    private string username = "";
    private string otpCode = "";
    private List<DeviceInfo> activatedDevices = new();
    
    protected override async Task OnInitializedAsync()
    {
        await LoadActivatedDevices();
    }
    
    private async Task GenerateActivationCode()
    {
        var request = new ActivationRequest
        {
            DeviceId = deviceId,
            Username = username,
            Issuer = "MyApp"
        };
        
        var result = await DeviceService.RequestDeviceActivationAsync(request);
        
        if (result.Success)
        {
            otpCode = result.OtpCode ?? "";
            deviceId = result.DeviceId ?? "";
        }
    }
    
    private async Task LoadActivatedDevices()
    {
        activatedDevices = (await DeviceService.GetActivatedDevicesAsync()).ToList();
    }
    
    private async Task DeactivateDevice(string deviceId)
    {
        await DeviceService.DeactivateDeviceAsync(deviceId);
        await LoadActivatedDevices();
    }
}
```

### 4. API Controller Example

Complete API controller for device management:

```csharp
using Microsoft.AspNetCore.Mvc;
using TwoFactorAuth.Core.Models;
using TwoFactorAuth.Core.Services;

namespace YourApp.Controllers;

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
        _deviceService = deviceService;
        _logger = logger;
    }
    
    /// <summary>
    /// Request device activation - generates OTP code
    /// </summary>
    [HttpPost("request-activation")]
    public async Task<ActionResult<ActivationResponse>> RequestActivation(
        [FromBody] ActivationRequest request)
    {
        if (string.IsNullOrEmpty(request.DeviceId))
        {
            return BadRequest(new { message = "Device ID is required" });
        }
        
        var result = await _deviceService.RequestDeviceActivationAsync(request);
        return Ok(result);
    }
    
    /// <summary>
    /// Activate device with OTP code
    /// </summary>
    [HttpPost("activate")]
    public async Task<ActionResult<ActivationValidationResult>> ActivateDevice(
        [FromBody] DeviceActivationValidation validation)
    {
        if (string.IsNullOrEmpty(validation.DeviceId) || 
            string.IsNullOrEmpty(validation.OtpCode))
        {
            return BadRequest(new { message = "Device ID and OTP code are required" });
        }
        
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
    public async Task<ActionResult<IEnumerable<DeviceInfo>>> GetActivatedDevices()
    {
        var devices = await _deviceService.GetActivatedDevicesAsync();
        return Ok(devices);
    }
    
    /// <summary>
    /// Deactivate a device
    /// </summary>
    [HttpDelete("{deviceId}")]
    public async Task<ActionResult> DeactivateDevice(string deviceId)
    {
        var result = await _deviceService.DeactivateDeviceAsync(deviceId);
        
        if (result)
        {
            return Ok(new { message = "Device deactivated successfully" });
        }
        
        return NotFound(new { message = "Device not found" });
    }
}
```

## Storage

The default implementation uses in-memory storage (`ConcurrentDictionary`). For production use, implement a database-backed storage:

```csharp
public class DatabaseDeviceManagementService : IDeviceManagementService
{
    private readonly ApplicationDbContext _dbContext;
    
    public DatabaseDeviceManagementService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    // Implement interface methods using Entity Framework Core
}
```

## Security Considerations

- Store device secrets securely in a database
- Use HTTPS for all API communications
- Implement rate limiting on activation endpoints
- Add authentication/authorization to device management endpoints
- Consider implementing device certificate pinning
- Rotate activation secrets periodically
- Implement audit logging for device activations

## Integration with Flutter SDK

This package works seamlessly with the Flutter SDK:

1. Flutter app calls `/api/device/request-activation`
2. Server generates OTP and displays it on web interface
3. User enters OTP on Flutter app
4. Flutter app calls `/api/device/activate` with OTP
5. Server validates OTP and activates device

## License

AGPL-3.0-or-later

## Author

Part of the 2FAPrivate project
