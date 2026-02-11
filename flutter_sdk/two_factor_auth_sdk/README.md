# Two Factor Auth SDK

A Flutter SDK for implementing two-factor authentication with device management capabilities. This SDK provides device identification, registration, whitelist management, and OTP-based device activation.

**[Tài liệu tiếng Việt](README_VI.md)** | [English Documentation](README.md)

## Features

- 🔐 **Device Identification**: Get unique device IDs for Android and iOS
- 📱 **Device Management**: Whitelist and manage authorized devices
- 🔑 **OTP Generation**: Generate TOTP and HOTP codes
- ✅ **OTP Validation**: Validate OTP codes with time window support
- 🚀 **Device Activation**: OTP-based device activation flow
- 💾 **Local Storage**: Persist device information and whitelist
- 🌐 **API Integration**: Optional integration with backend API
- 💼 **Transaction Authentication**: Specialized manager for stock trading and financial apps

## Installation

Add this to your package's `pubspec.yaml` file:

```yaml
dependencies:
  two_factor_auth_sdk:
    path: ../flutter_sdk/two_factor_auth_sdk
```

Or if published:

```yaml
dependencies:
  two_factor_auth_sdk: ^1.0.0
```

Then run:

```bash
flutter pub get
```

## Platform Configuration

### Android

Add to `android/app/src/main/AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.INTERNET" />
```

### iOS

No additional configuration required.

## Usage

### 1. Device Management

```dart
import 'package:two_factor_auth_sdk/two_factor_auth_sdk.dart';

// Create device manager
final deviceManager = DeviceManager();

// Get device ID
final deviceId = await deviceManager.getDeviceId();
print('Device ID: $deviceId');

// Get full device information
final deviceInfo = await deviceManager.getDeviceInfo();
print('Device: ${deviceInfo.model} (${deviceInfo.platform} ${deviceInfo.osVersion})');

// Check if device is whitelisted
final isWhitelisted = await deviceManager.isCurrentDeviceWhitelisted();
print('Is whitelisted: $isWhitelisted');

// Add device to whitelist
await deviceManager.addToWhitelist(deviceInfo);

// Get all whitelisted devices
final whitelistedDevices = await deviceManager.getWhitelistedDevices();
print('Whitelisted devices: ${whitelistedDevices.length}');
```

### 2. OTP Generation and Validation

```dart
import 'package:two_factor_auth_sdk/two_factor_auth_sdk.dart';

// Create OTP manager
final otpManager = OtpManager();

// Generate a secret
final secret = otpManager.generateSecret();
print('Secret: $secret');

// Generate TOTP code
final totpCode = otpManager.generateTotp(secret);
print('TOTP Code: $totpCode');

// Validate TOTP code
final isValid = otpManager.validateTotp(secret, totpCode);
print('Is valid: $isValid');

// Get remaining time for current code
final remainingTime = otpManager.getRemainingTime();
print('Code expires in: $remainingTime seconds');

// Generate provisioning URI for QR code
final uri = otpManager.generateProvisioningUri(
  secret: secret,
  issuer: 'MyApp',
  account: 'user@example.com',
);
print('Provisioning URI: $uri');
```

### 3. Device Activation Flow

```dart
import 'package:two_factor_auth_sdk/two_factor_auth_sdk.dart';

// Create device activation manager
final deviceActivation = DeviceActivation(
  baseUrl: 'https://your-api-server.com', // Optional
);

// Step 1: Request activation from server
final activationRequest = await deviceActivation.requestActivation(
  username: 'user@example.com',
  issuer: 'MyApp',
);

if (activationRequest['success']) {
  print('Activation OTP: ${activationRequest['otpCode']}');
  print('This code should be displayed on the web interface');
  
  // Step 2: User enters the OTP code shown on web interface
  final otpCode = '123456'; // User input
  
  // Step 3: Activate device with OTP code
  final response = await deviceActivation.activateDevice(
    otpCode: otpCode,
    secret: activationRequest['secret'],
    username: 'user@example.com',
    issuer: 'MyApp',
  );
  
  if (response.success) {
    print('Device activated successfully!');
  } else {
    print('Activation failed: ${response.message}');
  }
}

// Check if device is activated
final isActivated = await deviceActivation.isDeviceActivated();
print('Device activated: $isActivated');

// Verify device access before allowing sensitive operations
final hasAccess = await deviceActivation.verifyDeviceAccess();
if (hasAccess) {
  print('Device has access');
  // Allow user to proceed
} else {
  print('Device not authorized');
  // Show activation screen
}
```

### 4. Complete Example

For a complete stock trading application example with Vietnamese UI, see:
- **Stock Trading Example**: [example/stock_trading_example.dart](example/stock_trading_example.dart)
- **Example Documentation**: [example/README.md](example/README.md)

```dart
import 'package:flutter/material.dart';
import 'package:two_factor_auth_sdk/two_factor_auth_sdk.dart';

class DeviceActivationScreen extends StatefulWidget {
  @override
  _DeviceActivationScreenState createState() => _DeviceActivationScreenState();
}

class _DeviceActivationScreenState extends State<DeviceActivationScreen> {
  final _deviceActivation = DeviceActivation();
  final _otpController = TextEditingController();
  String? _secret;
  String? _otpCode;
  bool _isLoading = false;
  String? _message;

  Future<void> _requestActivation() async {
    setState(() {
      _isLoading = true;
      _message = null;
    });

    try {
      final result = await _deviceActivation.requestActivation(
        username: 'user@example.com',
        issuer: 'MyApp',
      );

      setState(() {
        _secret = result['secret'];
        _otpCode = result['otpCode'];
        _message = 'Enter the OTP code shown on the web interface: $_otpCode';
      });
    } catch (e) {
      setState(() {
        _message = 'Error: $e';
      });
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  Future<void> _activateDevice() async {
    setState(() {
      _isLoading = true;
      _message = null;
    });

    try {
      final response = await _deviceActivation.activateDevice(
        otpCode: _otpController.text,
        secret: _secret,
        username: 'user@example.com',
        issuer: 'MyApp',
      );

      setState(() {
        _message = response.message;
      });

      if (response.success) {
        // Navigate to home screen
        Navigator.pushReplacementNamed(context, '/home');
      }
    } catch (e) {
      setState(() {
        _message = 'Error: $e';
      });
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Device Activation')),
      body: Padding(
        padding: EdgeInsets.all(16.0),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            if (_secret == null) ...[
              Text('Activate your device to continue'),
              SizedBox(height: 20),
              ElevatedButton(
                onPressed: _isLoading ? null : _requestActivation,
                child: Text('Request Activation'),
              ),
            ] else ...[
              Text('Enter OTP Code'),
              SizedBox(height: 10),
              TextField(
                controller: _otpController,
                decoration: InputDecoration(
                  labelText: 'OTP Code',
                  border: OutlineInputBorder(),
                ),
                keyboardType: TextInputType.number,
                maxLength: 6,
              ),
              SizedBox(height: 20),
              ElevatedButton(
                onPressed: _isLoading ? null : _activateDevice,
                child: Text('Activate Device'),
              ),
            ],
            if (_message != null) ...[
              SizedBox(height: 20),
              Text(_message!, style: TextStyle(color: Colors.blue)),
            ],
            if (_isLoading) ...[
              SizedBox(height: 20),
              CircularProgressIndicator(),
            ],
          ],
        ),
      ),
    );
  }
}
```

## API Integration

The SDK supports optional integration with a backend API. Set the `baseUrl` parameter when creating `DeviceActivation`:

```dart
final deviceActivation = DeviceActivation(
  baseUrl: 'https://your-api-server.com',
);
```

### Required API Endpoints

- `POST /api/device/request-activation`: Request device activation
- `POST /api/device/activate`: Activate device with OTP code

See the NodeJS and .NET packages for backend implementation.

## License

AGPL-3.0-or-later

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
