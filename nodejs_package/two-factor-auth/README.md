# Two Factor Auth - NodeJS/TypeScript Package

A NodeJS/TypeScript package for implementing two-factor authentication with device management. This package provides reusable OTP generation, validation, and device activation services for NodeJS applications.

## Features

- 🔐 **Device Management**: Manage and whitelist authorized devices
- 🔑 **OTP Generation**: Generate TOTP codes for device activation
- ✅ **OTP Validation**: Validate OTP codes with time window support
- 🚀 **Device Activation Flow**: Complete OTP-based device activation
- 💾 **In-Memory Storage**: Built-in in-memory storage (replaceable with database)
- 📦 **TypeScript Support**: Full TypeScript support with type definitions

## Installation

```bash
npm install @tbcf/two-factor-auth
```

Or with yarn:

```bash
yarn add @tbcf/two-factor-auth
```

## Dependencies

- `otplib`: OTP generation and validation
- `uuid`: Generate unique activation tokens

## Usage

### 1. Basic Setup

```typescript
import { DeviceManagementService, OtpManager } from '@tbcf/two-factor-auth';

// Create instances
const otpManager = new OtpManager();
const deviceService = new DeviceManagementService(otpManager);
```

### 2. Device Activation Flow

#### Step 1: Request Activation

When a device wants to activate, request an activation OTP:

```typescript
const activationRequest = {
  deviceId: 'device-12345',
  deviceName: 'iPhone 13',
  platform: 'iOS',
  osVersion: '17.0',
  model: 'iPhone 13 Pro',
  username: 'user@example.com',
  issuer: 'MyApp'
};

const response = await deviceService.requestDeviceActivation(activationRequest);

if (response.success) {
  console.log('Activation OTP:', response.otpCode);
  console.log('Display this code on your web interface');
  // Store the secret for later validation
  console.log('Secret:', response.secret);
}
```

#### Step 2: Validate Activation

When the user enters the OTP code on their device:

```typescript
const validation = {
  deviceId: 'device-12345',
  otpCode: '123456', // Code entered by user
  username: 'user@example.com',
  issuer: 'MyApp'
};

const result = await deviceService.validateDeviceActivation(validation);

if (result.success) {
  console.log('Device activated!');
  console.log('Activation token:', result.activationToken);
} else {
  console.log('Activation failed:', result.message);
}
```

### 3. Device Management

```typescript
// Check if device is activated
const isActivated = await deviceService.isDeviceActivated('device-12345');

// Get device information
const deviceInfo = await deviceService.getDeviceInfo('device-12345');

// Get all activated devices
const activatedDevices = await deviceService.getActivatedDevices();

// Deactivate a device
await deviceService.deactivateDevice('device-12345');
```

### 4. OTP Generation

```typescript
import { OtpManager } from '@tbcf/two-factor-auth';

const otpManager = new OtpManager(6, 30); // 6 digits, 30 seconds period

// Generate a secret
const secret = otpManager.generateSecret();

// Generate TOTP code
const code = otpManager.generateTotp(secret);

// Validate TOTP code
const isValid = otpManager.validateTotp(secret, code);

// Generate provisioning URI for QR code
const uri = otpManager.generateProvisioningUri({
  secret,
  issuer: 'MyApp',
  account: 'user@example.com'
});

// Get remaining time for current code
const remainingTime = otpManager.getRemainingTime();
console.log(`Code expires in ${remainingTime} seconds`);
```

### 5. Express.js API Example

```typescript
import express from 'express';
import { DeviceManagementService } from '@tbcf/two-factor-auth';

const app = express();
app.use(express.json());

const deviceService = new DeviceManagementService();

// Request device activation
app.post('/api/device/request-activation', async (req, res) => {
  const response = await deviceService.requestDeviceActivation(req.body);
  
  if (response.success) {
    res.json(response);
  } else {
    res.status(400).json(response);
  }
});

// Activate device
app.post('/api/device/activate', async (req, res) => {
  const result = await deviceService.validateDeviceActivation(req.body);
  
  if (result.success) {
    res.json(result);
  } else {
    res.status(400).json(result);
  }
});

// Get device status
app.get('/api/device/:deviceId/status', async (req, res) => {
  const isActivated = await deviceService.isDeviceActivated(req.params.deviceId);
  const deviceInfo = await deviceService.getDeviceInfo(req.params.deviceId);
  
  res.json({
    deviceId: req.params.deviceId,
    isActivated,
    deviceInfo
  });
});

// Get all activated devices
app.get('/api/device/activated', async (req, res) => {
  const devices = await deviceService.getActivatedDevices();
  res.json(devices);
});

// Deactivate device
app.delete('/api/device/:deviceId', async (req, res) => {
  const result = await deviceService.deactivateDevice(req.params.deviceId);
  
  if (result) {
    res.json({ message: 'Device deactivated successfully' });
  } else {
    res.status(404).json({ message: 'Device not found' });
  }
});

app.listen(3000, () => {
  console.log('Server running on port 3000');
});
```

### 6. NestJS Example

```typescript
import { Injectable } from '@nestjs/common';
import { DeviceManagementService as TwoFactorDeviceService } from '@tbcf/two-factor-auth';

@Injectable()
export class DeviceService {
  private readonly deviceService: TwoFactorDeviceService;

  constructor() {
    this.deviceService = new TwoFactorDeviceService();
  }

  async requestActivation(data: any) {
    return this.deviceService.requestDeviceActivation(data);
  }

  async validateActivation(data: any) {
    return this.deviceService.validateDeviceActivation(data);
  }

  async isDeviceActivated(deviceId: string) {
    return this.deviceService.isDeviceActivated(deviceId);
  }
}
```

## API Reference

### DeviceManagementService

#### Methods

- `requestDeviceActivation(request: ActivationRequest): Promise<ActivationResponse>`
  - Request device activation and generate OTP code
  
- `validateDeviceActivation(validation: DeviceActivationValidation): Promise<ActivationValidationResult>`
  - Validate device activation with OTP code
  
- `isDeviceActivated(deviceId: string): Promise<boolean>`
  - Check if device is activated
  
- `getDeviceInfo(deviceId: string): Promise<DeviceInfo | null>`
  - Get device information
  
- `getActivatedDevices(): Promise<DeviceInfo[]>`
  - Get all activated devices
  
- `deactivateDevice(deviceId: string): Promise<boolean>`
  - Deactivate a device

### OtpManager

#### Methods

- `generateSecret(): string`
  - Generate a random secret key
  
- `generateTotp(secret: string): string`
  - Generate TOTP code
  
- `validateTotp(secret: string, code: string, window?: number): boolean`
  - Validate TOTP code
  
- `generateProvisioningUri(params: {secret, issuer, account}): string`
  - Generate provisioning URI for QR code
  
- `getRemainingTime(): number`
  - Get remaining time for current TOTP code (in seconds)

## Storage

The default implementation uses in-memory storage (`Map`). For production use, extend the `DeviceManagementService` class and implement database storage:

```typescript
import { DeviceManagementService } from '@tbcf/two-factor-auth';

class DatabaseDeviceManagementService extends DeviceManagementService {
  constructor(private database: any) {
    super();
  }

  // Override methods to use database
  async getDeviceInfo(deviceId: string) {
    return this.database.findDevice(deviceId);
  }

  // ... implement other methods
}
```

## Security Considerations

- Store device secrets securely in a database
- Use HTTPS for all API communications
- Implement rate limiting on activation endpoints
- Add authentication/authorization to device management endpoints
- Implement audit logging for device activations
- Consider implementing device certificate pinning
- Rotate activation secrets periodically

## Integration with Flutter SDK and .NET Package

This package is part of a multi-platform 2FA solution:

- **Flutter SDK**: Mobile device implementation
- **.NET Core Package**: Blazor web application implementation
- **NodeJS Package**: NodeJS/Express/NestJS backend implementation

All packages use the same device activation flow for consistency.

## TypeScript Types

The package includes full TypeScript type definitions:

```typescript
interface DeviceInfo {
  deviceId: string;
  deviceName: string;
  platform: string;
  osVersion: string;
  model: string;
  registeredAt: Date;
  isActivated: boolean;
  activatedAt?: Date;
  activationToken?: string;
}

interface ActivationRequest {
  deviceId: string;
  deviceName?: string;
  platform?: string;
  osVersion?: string;
  model?: string;
  username: string;
  issuer: string;
}

// ... and more
```

## Building from Source

```bash
# Clone the repository
git clone https://github.com/TBCF-Company/2FAPrivate.git

# Navigate to the NodeJS package
cd nodejs_package/two-factor-auth

# Install dependencies
npm install

# Build TypeScript
npm run build

# The compiled JavaScript will be in the dist/ folder
```

## License

AGPL-3.0-or-later

## Author

Part of the 2FAPrivate project by TBCF Company

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
