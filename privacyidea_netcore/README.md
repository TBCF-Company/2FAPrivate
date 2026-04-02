# PrivacyIDEA .NET Core 8

A .NET Core 8 port of the [PrivacyIDEA](https://privacyidea.org/) multi-factor authentication system.

## Project Structure

```
privacyidea_netcore/
├── src/
│   ├── PrivacyIDEA.Api/           # ASP.NET Core Web API
│   ├── PrivacyIDEA.Core/          # Business Logic & Services
│   │   ├── EventHandlers/         # Event handler implementations
│   │   ├── Interfaces/            # Service interfaces
│   │   ├── Resolvers/             # User resolver implementations
│   │   ├── Services/              # Service implementations
│   │   ├── SmsProviders/          # SMS provider implementations
│   │   └── Tokens/                # Token type implementations
│   ├── PrivacyIDEA.Domain/        # Domain Entities & Enums
│   ├── PrivacyIDEA.Infrastructure/ # Data Access (EF Core)
│   └── PrivacyIDEA.Cli/           # CLI Tools (pi-manage)
└── tests/
    └── PrivacyIDEA.Core.Tests/    # Unit Tests
```

## Features

### Token Types (25+)
- **OTP Tokens**: HOTP, TOTP, mOTP
- **Challenge-Response**: SMS, Email, Push, TiQR
- **WebAuthn/FIDO**: WebAuthn, Passkey, U2F
- **Hardware**: Yubico, YubiKey, Daplug, VASCO
- **Special**: Certificate, SSH Key, Password, RADIUS, OCRA
- **Utility**: Paper, TAN, IndexedSecret, Registration

### User Resolvers (7)
- LDAP (Active Directory, OpenLDAP)
- SQL (MySQL, PostgreSQL, SQLite, SQL Server)
- Microsoft Entra ID (Azure AD)
- SCIM (System for Cross-domain Identity Management)
- HTTP/REST
- Passwd file
- JSON/YAML file

### Event Handlers (6)
- User Notification (Email, SMS)
- Token Handler (enable, disable, delete, etc.)
- Webhook
- Counter
- Script
- Logging

### SMS Providers (5)
- HTTP Gateway
- SMPP
- Twilio
- AWS SNS
- Console (development)

## Getting Started

### Prerequisites
- .NET 8 SDK
- Database (SQLite, MySQL, PostgreSQL, or SQL Server)

### Build
```bash
cd privacyidea_netcore
dotnet restore
dotnet build
```

### Create Database
```bash
dotnet run --project src/PrivacyIDEA.Cli -- create-tables --connection "Data Source=privacyidea.db" --provider sqlite
```

### Create Encryption Key
```bash
dotnet run --project src/PrivacyIDEA.Cli -- create-enckey --file enckey
```

### Add Admin User
```bash
dotnet run --project src/PrivacyIDEA.Cli -- admin add --username admin --email admin@example.com --connection "Data Source=privacyidea.db"
```

### Run API Server
```bash
cd src/PrivacyIDEA.Api
dotnet run
```

API will be available at `https://localhost:5001`

### Run Tests
```bash
dotnet test
```

## API Endpoints

| Endpoint | Description |
|----------|-------------|
| `POST /validate/check` | Validate OTP |
| `POST /validate/triggerchallenge` | Trigger challenge-response |
| `GET /token` | List tokens |
| `POST /token/init` | Initialize new token |
| `GET /realm` | List realms |
| `GET /audit` | Query audit log |
| `GET /system` | System configuration |

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=privacyidea.db"
  },
  "PrivacyIDEA": {
    "EncryptionKeyFile": "enckey",
    "DefaultRealm": "default"
  }
}
```

## Library Mappings

| Python | .NET Core |
|--------|-----------|
| Flask | ASP.NET Core |
| SQLAlchemy | Entity Framework Core 8 |
| cryptography | System.Security.Cryptography |
| argon2_cffi | Konscious.Security.Cryptography.Argon2 |
| ldap3 | Novell.Directory.Ldap.NETStandard |
| webauthn | Fido2 |
| PyJWT | System.IdentityModel.Tokens.Jwt |

## License

This project is a port of PrivacyIDEA which is licensed under AGPLv3.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `dotnet test`
5. Submit a pull request

## Acknowledgments

- [PrivacyIDEA](https://privacyidea.org/) - Original Python implementation
- [NetKnights GmbH](https://netknights.it/) - Original developers
