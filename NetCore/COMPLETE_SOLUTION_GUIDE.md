# Complete 2FA Solution - Setup and Usage Guide

This guide explains how to set up and use the complete two-factor authentication (2FA) solution consisting of:
1. ASP.NET Core Web API (Backend)
2. Blazor Web Application (Frontend)
3. Flutter Mobile App (Mobile Client)

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                   User Flow                          │
└─────────────────────────────────────────────────────┘

 Web Browser (Blazor)           Mobile App (Flutter)
 ┌──────────────────┐          ┌──────────────────┐
 │  1. Enroll 2FA   │          │                  │
 │  2. Get QR Code  │          │                  │
 │  3. Show Secret  │ ───────► │ 4. Scan/Enter    │
 │                  │          │    Secret Key    │
 │                  │          │ 5. Generate OTP  │
 │  6. Enter OTP    │ ◄─────── │ 6. Display OTP   │
 │  7. Validate     │          │                  │
 └────────┬─────────┘          └──────────────────┘
          │
          │ REST API
          ▼
 ┌──────────────────┐
 │  ASP.NET Core    │
 │  Web API         │
 │  (OTP Service)   │
 └──────────────────┘
```

## Prerequisites

### .NET Development
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code
- Git

### Flutter Development (Optional)
- Flutter SDK 3.0+
- Android Studio or Xcode
- Android/iOS Emulator or physical device

## Quick Start

### 1. Clone the Repository

```bash
git clone <repository-url>
cd 2FAPrivate/NetCore
```

### 2. Build the Solution

```bash
dotnet build TwoFactorAuth.sln
```

### 3. Run the API Server

```bash
cd PrivacyIdeaServer
dotnet run
```

The API will start on `http://localhost:5001` (or `https://localhost:5001`)

### 4. Run the Blazor Web App

Open a new terminal:

```bash
cd BlazorWebApp
dotnet run
```

The web app will start on `http://localhost:5002` (check the console output for the exact URL)

### 5. Setup Flutter App (Optional)

```bash
cd ../../flutter
flutter pub get
flutter run
```

## Using the Solution

### Scenario: Stock Trading App Style 2FA

This solution demonstrates a 2FA flow similar to stock trading applications:

#### Step 1: Enroll on Web

1. Open the Blazor web app in your browser
2. Navigate to "Two-Factor Auth" page
3. Enter your username (e.g., `user@example.com`)
4. Click "Enroll Device"
5. System generates a secret key and QR code
6. **Copy the Secret Key** (you'll need it for the mobile app)

#### Step 2: Setup Mobile App

1. Open the Flutter mobile app
2. Tap the **+** button
3. Enter the details:
   - Account: your username
   - Issuer: "2FA Demo App"
   - Secret Key: paste the secret from step 1
4. Tap "Add Token"
5. Tap the token to view OTP codes

#### Step 3: Validate on Web

1. Back in the web app, click "Continue to Validation"
2. Look at your mobile app - it shows a 6-digit code
3. Enter this code in the web app
4. Click "Validate Code"
5. ✓ Success! Your 2FA is verified

#### Step 4: Test Transaction (Simulation)

1. On the web app, you can generate current OTP for testing
2. The mobile app automatically refreshes codes every 30 seconds
3. In a real scenario:
   - User initiates a sensitive action (transfer money, place order)
   - Web shows: "Please confirm with OTP from your mobile app"
   - User opens mobile app, enters current OTP
   - Transaction is authorized

## API Endpoints

### Base URL: `http://localhost:5001/api/otp`

#### 1. Enroll Token

**POST** `/api/otp/enroll`

Request:
```json
{
  "username": "user@example.com",
  "issuer": "2FA Demo App",
  "isTotp": true,
  "digits": 6,
  "step": 30
}
```

Response:
```json
{
  "secret": "JBSWY3DPEHPK3PXP",
  "provisioningUri": "otpauth://totp/2FA%20Demo%20App:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=2FA%20Demo%20App&algorithm=SHA1&digits=6&period=30",
  "qrCodeDataUrl": "..."
}
```

#### 2. Validate OTP

**POST** `/api/otp/validate`

Request:
```json
{
  "secret": "JBSWY3DPEHPK3PXP",
  "code": "123456",
  "isTotp": true,
  "digits": 6,
  "step": 30
}
```

Response:
```json
{
  "isValid": true,
  "message": "OTP code is valid"
}
```

#### 3. Generate OTP (Testing)

**POST** `/api/otp/generate`

Request:
```json
{
  "secret": "JBSWY3DPEHPK3PXP",
  "isTotp": true,
  "digits": 6,
  "step": 30
}
```

Response:
```json
{
  "code": "123456",
  "timestamp": "2026-02-06T13:30:00Z"
}
```

## Project Structure

### ASP.NET Core API

```
PrivacyIdeaServer/
├── Controllers/
│   └── OtpController.cs          # REST API endpoints
├── Services/
│   └── OtpTokenService.cs        # OTP generation/validation
├── Models/                        # Database models
├── Lib/                          # Core libraries
└── Program.cs                    # DI configuration
```

### Blazor Web App

```
BlazorWebApp/
├── Components/
│   ├── Pages/
│   │   └── TwoFactorAuth.razor   # 2FA enrollment page
│   └── Layout/
│       └── NavMenu.razor         # Navigation
├── wwwroot/                      # Static assets
└── Program.cs                    # App configuration
```

### Flutter Mobile App

```
flutter/
├── lib/
│   ├── main.dart                 # App entry point
│   ├── models/
│   │   └── token_data.dart       # Token model
│   ├── services/
│   │   └── otp_service.dart      # OTP service
│   └── screens/
│       ├── home_screen.dart      # Token list
│       ├── otp_display_screen.dart  # OTP display
│       └── qr_scanner_screen.dart   # Add token
└── pubspec.yaml                  # Dependencies
```

## Dependency Injection Setup

The solution uses .NET's built-in DI container. Services are registered in `Program.cs`:

```csharp
// Register OTP service
builder.Services.AddScoped<IOtpTokenService, OtpTokenService>();

// Register database context
builder.Services.AddDbContext<PrivacyIDEAContext>(options =>
    options.UseSqlite(connectionString));
```

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8.0
- **ORM**: Entity Framework Core 8.0
- **Database**: SQLite (can use any EF-supported DB)
- **OTP Library**: Otp.NET
- **API Documentation**: Swagger/OpenAPI

### Frontend (Web)
- **Framework**: Blazor Server
- **UI**: Bootstrap 5
- **HTTP Client**: Built-in HttpClient

### Mobile
- **Framework**: Flutter 3.0+
- **State Management**: StatefulWidget
- **OTP Library**: otp package
- **Storage**: shared_preferences
- **HTTP**: http package

## Security Considerations

### Production Checklist

- [ ] Use HTTPS everywhere
- [ ] Store secrets encrypted
- [ ] Implement rate limiting
- [ ] Add user authentication
- [ ] Use secure database (PostgreSQL/SQL Server)
- [ ] Implement session management
- [ ] Add logging and monitoring
- [ ] Validate all inputs
- [ ] Implement CORS properly
- [ ] Use secure random number generation
- [ ] Add biometric authentication (mobile)
- [ ] Implement token backup/recovery
- [ ] Add fraud detection

### Current Limitations (Demo)

- No user authentication
- Secrets transmitted in plain text
- No persistent storage
- No encryption at rest
- No audit logging
- No rate limiting
- CORS allows all origins

## Testing

### Unit Tests

```bash
cd PrivacyIdeaServer
dotnet test
```

### Integration Testing

1. Start API: `dotnet run --project PrivacyIdeaServer`
2. Test enrollment endpoint:
```bash
curl -X POST http://localhost:5001/api/otp/enroll \
  -H "Content-Type: application/json" \
  -d '{"username":"test@example.com","issuer":"Test"}'
```

### End-to-End Testing

1. Run API server
2. Run Blazor app
3. Enroll token on web
4. Copy secret
5. Add token to mobile app (or use web testing feature)
6. Validate OTP

## Troubleshooting

### API Not Starting

- Check port 5001 is not in use
- Verify .NET 8.0 SDK is installed: `dotnet --version`
- Check firewall settings

### Blazor App Not Connecting to API

- Update API URL in TwoFactorAuth.razor
- Check CORS settings in API
- Verify API is running

### Flutter App Build Issues

- Run `flutter doctor` to check setup
- Run `flutter clean` and `flutter pub get`
- Update Flutter SDK: `flutter upgrade`

### OTP Codes Don't Match

- Verify secret key is correct
- Check time synchronization (TOTP requires accurate time)
- Ensure same parameters (digits, period) on both sides

## Next Steps

### Enhancements

1. **Add more Python modules to C#**
   - Token types (SMS, Email, WebAuthn)
   - Policy engine
   - User management
   - Audit logging

2. **Improve Security**
   - Add authentication/authorization
   - Implement JWT tokens
   - Encrypt secrets
   - Add HTTPS enforcement

3. **Add Features**
   - QR code scanning in Flutter
   - Push notifications
   - Backup codes
   - Multiple devices

4. **Production Ready**
   - Add comprehensive tests
   - Implement CI/CD
   - Add monitoring
   - Deploy to cloud

## References

- [RFC 6238 - TOTP](https://tools.ietf.org/html/rfc6238)
- [RFC 4226 - HOTP](https://tools.ietf.org/html/rfc4226)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Flutter Documentation](https://flutter.dev/docs)
- [Otp.NET Library](https://github.com/kspearrin/Otp.NET)

## License

AGPL-3.0-or-later

## Support

For issues and questions, please refer to the repository documentation.
