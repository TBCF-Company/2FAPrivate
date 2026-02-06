# 2FA Device Management Solution

A comprehensive two-factor authentication solution with device management across multiple platforms (Flutter, .NET/Blazor, and NodeJS).

## Overview

This solution provides a complete 2FA system with device management capabilities:

- **Flutter SDK**: Mobile device implementation with device ID detection, OTP generation, and activation flow
- **.NET Core Package**: Reusable library for Blazor and ASP.NET Core applications
- **NodeJS Package**: TypeScript package for NodeJS/Express/NestJS applications

## Architecture

### Device Activation Flow

1. **Device Registration**: Mobile device requests activation with device information
2. **OTP Generation**: Server generates and displays OTP code on web interface
3. **OTP Verification**: User enters OTP code on mobile device
4. **Activation**: Server validates OTP and activates device
5. **Whitelist Management**: Only activated devices can access protected resources

```
┌─────────────────┐          ┌─────────────────┐
│  Mobile Device  │          │  Web/Admin UI   │
│   (Flutter)     │          │   (Blazor)      │
└────────┬────────┘          └────────┬────────┘
         │                            │
         │ 1. Request Activation      │
         ├──────────────────────────>│
         │    (Device ID + Info)      │
         │                            │
         │                    2. Generate OTP
         │                       Display on UI
         │                            │
         │ 3. User sees OTP           │
         │    and enters on device    │
         │                            │
         │ 4. Submit OTP Code         │
         ├──────────────────────────>│
         │                            │
         │                    5. Validate OTP
         │                    6. Activate Device
         │                            │
         │ 7. Activation Success      │
         │<──────────────────────────┤
         │    (Activation Token)      │
         │                            │
```

## Components

### 1. Flutter SDK (`flutter_sdk/two_factor_auth_sdk`)

Reusable Flutter package for mobile device management and 2FA.

**Features:**
- Cross-platform device ID detection (Android & iOS)
- Device registration and whitelist management
- TOTP/HOTP generation
- Device activation with OTP verification
- Local storage of device information

**Installation:**
```yaml
dependencies:
  two_factor_auth_sdk:
    path: ../flutter_sdk/two_factor_auth_sdk
```

**Usage:**
```dart
import 'package:two_factor_auth_sdk/two_factor_auth_sdk.dart';

// Get device ID
final deviceManager = DeviceManager();
final deviceId = await deviceManager.getDeviceId();

// Request activation
final deviceActivation = DeviceActivation(
  baseUrl: 'https://your-api.com',
);
final result = await deviceActivation.requestActivation(
  username: 'user@example.com',
  issuer: 'MyApp',
);

// Activate with OTP
final response = await deviceActivation.activateDevice(
  otpCode: '123456',
  secret: result['secret'],
);
```

See [Flutter SDK README](flutter_sdk/two_factor_auth_sdk/README.md) for full documentation.

### 2. .NET Core Package (`NetCore/TwoFactorAuth.Core`)

Reusable class library for ASP.NET Core and Blazor applications.

**Features:**
- Device management service
- OTP generation and validation
- Device activation flow
- In-memory storage (extendable to database)

**Installation:**
```xml
<ItemGroup>
  <ProjectReference Include="..\TwoFactorAuth.Core\TwoFactorAuth.Core.csproj" />
</ItemGroup>
```

**Usage:**
```csharp
// Register service
builder.Services.AddSingleton<IDeviceManagementService, DeviceManagementService>();

// Use in controller
public class DeviceController : ControllerBase
{
    private readonly IDeviceManagementService _deviceService;
    
    [HttpPost("request-activation")]
    public async Task<ActionResult> RequestActivation(ActivationRequest request)
    {
        var result = await _deviceService.RequestDeviceActivationAsync(request);
        return Ok(result);
    }
}
```

See [.NET Core Package README](NetCore/TwoFactorAuth.Core/README.md) for full documentation.

### 3. NodeJS Package (`nodejs_package/two-factor-auth`)

TypeScript package for NodeJS applications.

**Features:**
- Device management service
- TOTP generation and validation
- Device activation flow
- Full TypeScript support

**Installation:**
```bash
npm install @tbcf/two-factor-auth
```

**Usage:**
```typescript
import { DeviceManagementService } from '@tbcf/two-factor-auth';

const deviceService = new DeviceManagementService();

// Request activation
app.post('/api/device/request-activation', async (req, res) => {
  const result = await deviceService.requestDeviceActivation(req.body);
  res.json(result);
});

// Activate device
app.post('/api/device/activate', async (req, res) => {
  const result = await deviceService.validateDeviceActivation(req.body);
  res.json(result);
});
```

See [NodeJS Package README](nodejs_package/two-factor-auth/README.md) for full documentation.

## API Endpoints

All implementations provide the following REST API endpoints:

### POST /api/device/request-activation

Request device activation and generate OTP code.

**Request:**
```json
{
  "deviceId": "device-12345",
  "deviceName": "iPhone 13 Pro",
  "platform": "iOS",
  "osVersion": "17.0",
  "model": "iPhone13,3",
  "username": "user@example.com",
  "issuer": "MyApp"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Activation OTP generated",
  "otpCode": "123456",
  "secret": "JBSWY3DPEHPK3PXP",
  "deviceId": "device-12345"
}
```

### POST /api/device/activate

Activate device with OTP code.

**Request:**
```json
{
  "deviceId": "device-12345",
  "otpCode": "123456",
  "username": "user@example.com",
  "issuer": "MyApp"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Device activated successfully",
  "activationToken": "uuid-token",
  "activatedAt": "2025-02-06T15:00:00Z"
}
```

### GET /api/device/{deviceId}/status

Get device activation status.

**Response:**
```json
{
  "deviceId": "device-12345",
  "isActivated": true,
  "deviceInfo": {
    "deviceId": "device-12345",
    "deviceName": "iPhone 13 Pro",
    "platform": "iOS",
    "isActivated": true,
    "activatedAt": "2025-02-06T15:00:00Z"
  }
}
```

### GET /api/device/activated

Get all activated devices.

**Response:**
```json
[
  {
    "deviceId": "device-12345",
    "deviceName": "iPhone 13 Pro",
    "platform": "iOS",
    "isActivated": true,
    "activatedAt": "2025-02-06T15:00:00Z"
  }
]
```

### DELETE /api/device/{deviceId}

Deactivate a device.

**Response:**
```json
{
  "message": "Device deactivated successfully"
}
```

## Integration Example

### Flutter Mobile App + .NET Blazor Web + NodeJS Backend

1. **Flutter App** requests activation:
   ```dart
   final deviceActivation = DeviceActivation(
     baseUrl: 'https://your-api.com',
   );
   final result = await deviceActivation.requestActivation(
     username: 'user@example.com',
     issuer: 'MyApp',
   );
   ```

2. **Web Admin** displays OTP code (using .NET):
   ```csharp
   [HttpPost("request-activation")]
   public async Task<ActionResult> RequestActivation(ActivationRequest request)
   {
       var result = await _deviceService.RequestDeviceActivationAsync(request);
       // Display result.OtpCode on UI
       return Ok(result);
   }
   ```

3. **User** enters OTP on mobile device

4. **Flutter App** sends OTP for validation:
   ```dart
   final response = await deviceActivation.activateDevice(
     otpCode: userInput,
     secret: result['secret'],
   );
   ```

5. **Backend** (NodeJS or .NET) validates and activates device

## Security Considerations

- Store device secrets securely (use database encryption)
- Use HTTPS for all API communications
- Implement rate limiting on activation endpoints
- Add authentication/authorization to device management endpoints
- Implement audit logging for device activations
- Consider device certificate pinning
- Rotate activation secrets periodically
- Set appropriate OTP time windows (default: 30 seconds)

## Storage

All implementations use in-memory storage by default for simplicity. For production:

### .NET: Use Entity Framework Core
```csharp
public class DeviceDbContext : DbContext
{
    public DbSet<DeviceInfo> Devices { get; set; }
}
```

### NodeJS: Use MongoDB/PostgreSQL
```typescript
class DatabaseDeviceManagementService extends DeviceManagementService {
  async getDeviceInfo(deviceId: string) {
    return await this.database.findOne({ deviceId });
  }
}
```

### Flutter: Already uses SharedPreferences for local storage

## Testing

Each package includes examples and documentation for testing:

- **Flutter SDK**: See `flutter_sdk/two_factor_auth_sdk/README.md`
- **.NET Package**: See `NetCore/TwoFactorAuth.Core/README.md`
- **NodeJS Package**: See `nodejs_package/two-factor-auth/examples/basic-usage.ts`

## License

AGPL-3.0-or-later

## Contributing

Contributions are welcome! Please submit pull requests or open issues.

## Support

For questions or issues, please open an issue on GitHub.
