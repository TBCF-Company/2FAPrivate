# 2FA Mobile App (Flutter)

A Flutter mobile application for managing and generating two-factor authentication (2FA) codes.

## Features

- 📱 **Token Management**: Add and manage multiple 2FA tokens
- 🔐 **TOTP Generation**: Generate time-based one-time passwords (TOTP)
- ⏱️ **Real-time Display**: See current OTP code with countdown timer
- 💾 **Local Storage**: Securely store tokens on device
- 📋 **Copy to Clipboard**: Easy copy of OTP codes
- 🎨 **Modern UI**: Clean and intuitive Material Design interface

## Setup

### Prerequisites

- Flutter SDK (>=3.0.0)
- Android Studio / VS Code with Flutter extensions
- Android or iOS device/emulator

### Installation

1. Navigate to the flutter directory:
   ```bash
   cd flutter
   ```

2. Install dependencies:
   ```bash
   flutter pub get
   ```

3. Run the app:
   ```bash
   flutter run
   ```

## Usage

### Adding a Token

1. Tap the **+** button on the home screen
2. Enter token details:
   - **Account**: Your username or email
   - **Issuer**: Service name (e.g., "2FA Demo App")
   - **Secret Key**: The secret key from the web app
3. Tap **Add Token**

### Viewing OTP Codes

1. Tap on a token from the list
2. View the current 6-digit OTP code
3. Watch the countdown timer (30 seconds)
4. Tap **Copy Code** to copy to clipboard
5. The code refreshes automatically every 30 seconds

## Integration with Web App

This mobile app works with the Blazor web application:

1. **Web App**: Generate QR code and display secret key
2. **Mobile App**: Enter the secret key manually
3. **Mobile App**: Generate and display OTP codes
4. **Web App**: Use mobile-generated OTP to authenticate transactions

## Project Structure

```
flutter/
├── lib/
│   ├── main.dart                      # App entry point
│   ├── models/
│   │   └── token_data.dart            # Token data model
│   ├── screens/
│   │   ├── home_screen.dart           # Token list screen
│   │   ├── otp_display_screen.dart    # OTP display screen
│   │   └── qr_scanner_screen.dart     # Add token screen
│   └── services/
│       └── otp_service.dart           # OTP generation service
├── pubspec.yaml                       # Dependencies
└── README.md                          # This file
```

## Dependencies

- `http`: REST API communication
- `otp`: TOTP/HOTP generation
- `shared_preferences`: Local data storage
- `qr_flutter`: QR code display (future feature)
- `qr_code_scanner`: QR code scanning (future feature)

## Future Enhancements

- [ ] QR code scanner integration
- [ ] Biometric authentication
- [ ] Cloud backup/sync
- [ ] Multiple hash algorithms (SHA256, SHA512)
- [ ] HOTP support
- [ ] Token export/import
- [ ] Dark mode

## Security Notes

- Tokens are stored locally on the device
- Use secure storage for production apps
- Consider encryption for token secrets
- Implement biometric authentication
- Follow platform security best practices

## Testing

Run tests:
```bash
flutter test
```

## Build

Build for Android:
```bash
flutter build apk
```

Build for iOS:
```bash
flutter build ios
```

## License

AGPL-3.0-or-later

## Author

Converted from Python privacyIDEA to .NET/Flutter
