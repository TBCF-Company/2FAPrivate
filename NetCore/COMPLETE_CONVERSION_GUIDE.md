# Python to C# Conversion Guide - Complete Reference

## Overview

This document provides a comprehensive guide for converting the PrivacyIDEA Python codebase (Flask + SQLAlchemy) to C# (.NET Core 8). This is a large-scale conversion project with 361 Python files to convert.

## Conversion Strategy

### Phase-by-Phase Approach

1. **Phase 1: Core Infrastructure** ✅ COMPLETED
   - Database models (SQLAlchemy → Entity Framework Core)
   - Core configuration and utilities
   - Foundation for all other components

2. **Phase 2: Core Library** 🚧 IN PROGRESS
   - Business logic (lib/ directory)
   - Cryptography, token management, user resolution
   - Policy engine
   - Utility functions

3. **Phase 3: API Endpoints**
   - REST API controllers (api/ directory)
   - Flask blueprints → ASP.NET Core controllers
   - Request/response handling

4. **Phase 4: Supporting Services**
   - SMS/Email gateways
   - RADIUS integration
   - Event handlers
   - Background tasks

5. **Phase 5: Advanced Features**
   - FIDO2/WebAuthn support
   - CA connectors
   - Monitoring modules
   - CLI tools

## Library Mapping

### Core Framework

| Python | C# / .NET | Package | Notes |
|--------|-----------|---------|-------|
| Flask | ASP.NET Core | Built-in | Web framework |
| SQLAlchemy | Entity Framework Core | Microsoft.EntityFrameworkCore | ORM |
| Alembic | EF Core Migrations | Built-in | Database migrations |
| flask-sqlalchemy | - | - | Integrated in EF Core |
| flask-migrate | - | - | Use `dotnet ef` CLI |

### Authentication & Security

| Python | C# / .NET | Package | Notes |
|--------|-----------|---------|-------|
| cryptography | System.Security.Cryptography | Built-in | Core crypto operations |
| passlib | BCrypt.Net-Next | BCrypt.Net-Next 4.0.3 | Password hashing |
| argon2-cffi | Konscious.Security.Cryptography.Argon2 | Optional for Argon2 |
| pyopenssl | System.Security.Cryptography.X509Certificates | Built-in | TLS/SSL |
| bcrypt | BCrypt.Net-Next | BCrypt.Net-Next | bcrypt hashing |
| pyjwt | System.IdentityModel.Tokens.Jwt | Microsoft.IdentityModel.Tokens | JWT tokens |

### Directory Integration

| Python | C# / .NET | Package | Notes |
|--------|-----------|---------|-------|
| ldap3 | Novell.Directory.Ldap.NETStandard | Novell.Directory.Ldap.NETStandard 3.6.0 | LDAP client |
| pyrad | Flexinets.Radius.Core | Flexinets.Radius.Core 3.0.0 | RADIUS authentication |
| msal | Microsoft.Identity.Client | MSAL.NET | Azure AD integration |

### Communication & Messaging

| Python | C# / .NET | Package | Notes |
|--------|-----------|---------|-------|
| requests | HttpClient | Built-in | HTTP client |
| smtplib | System.Net.Mail.SmtpClient | Built-in | SMTP/Email |
| flask-babel | IStringLocalizer | Microsoft.Extensions.Localization | Internationalization |

### Data Handling

| Python | C# / .NET | Package | Notes |
|--------|-----------|---------|-------|
| cbor2 | PeterO.Cbor | PeterO.Cbor | CBOR encoding |
| protobuf | Google.Protobuf | Google.Protobuf | Protocol Buffers |
| grpcio | Grpc.Net.Client | Grpc.Net.Client | gRPC |
| feedparser | CodeHollow.FeedReader | Optional | RSS/Atom feeds |
| beautifulsoup4 | HtmlAgilityPack | HtmlAgilityPack | HTML parsing |

### Background Tasks

| Python | C# / .NET | Package | Notes |
|--------|-----------|---------|-------|
| huey[redis] | Hangfire | Hangfire.Core | Task queue (option 1) |
| huey[redis] | Quartz.NET | Quartz | Task scheduler (option 2) |
| croniter | NCrontab | NCrontab | Cron expression parsing |

### FIDO2/WebAuthn

| Python | C# / .NET | Package | Notes |
|--------|-----------|---------|-------|
| webauthn | Fido2.NetFramework | Fido2.NetFramework | WebAuthn/FIDO2 |

### Database Drivers

| Python | C# / .NET | Package | Notes |
|--------|-----------|---------|-------|
| pymysql | MySql.EntityFrameworkCore | MySql.EntityFrameworkCore | MySQL |
| psycopg2 | Npgsql.EntityFrameworkCore.PostgreSQL | Npgsql.EntityFrameworkCore.PostgreSQL | PostgreSQL |
| - | Microsoft.EntityFrameworkCore.SqlServer | Built-in | SQL Server |
| - | Microsoft.EntityFrameworkCore.Sqlite | Built-in | SQLite |

### Caching

| Python | C# / .NET | Package | Notes |
|--------|-----------|---------|-------|
| cachetools | IMemoryCache | Microsoft.Extensions.Caching.Memory | In-memory cache |
| redis-py | StackExchange.Redis | StackExchange.Redis | Redis cache |

## Code Conversion Patterns

### Database Models

**Python (SQLAlchemy):**
```python
class Token(db.Model):
    __tablename__ = 'token'
    id = db.Column(db.Integer, Sequence("token_seq"), primary_key=True)
    serial = db.Column(db.Unicode(40), unique=True, nullable=False)
    active = db.Column(db.Boolean, default=True)
    
    info_list = relationship('TokenInfo', lazy='select', cascade="all, delete-orphan")
```

**C# (Entity Framework Core):**
```csharp
[Table("token")]
[Index(nameof(Serial), IsUnique = true)]
public class Token : IMethodsMixin
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(40)]
    public string Serial { get; set; } = string.Empty;

    public bool Active { get; set; } = true;

    public ICollection<TokenInfo> InfoList { get; set; } = new List<TokenInfo>();
}
```

### API Endpoints

**Python (Flask Blueprint):**
```python
@token_blueprint.route('/<serial>', methods=['GET'])
@prepolicy(check_base_action, request, action=ACTION.TOKENLIST)
@log_with(log)
def get_token(serial=None):
    tokens = get_tokens(serial=serial)
    return send_result(tokens)
```

**C# (ASP.NET Core Controller):**
```csharp
[HttpGet("{serial}")]
[Authorize]
public async Task<IActionResult> GetToken(string serial)
{
    try
    {
        var tokens = await _tokenService.GetTokensAsync(serial);
        return Ok(new { result = new { status = true, value = tokens } });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error getting token {serial}");
        return StatusCode(500, new { result = new { status = false, error = ex.Message } });
    }
}
```

### Cryptography

**Python:**
```python
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes

def encrypt(data, iv):
    cipher = Cipher(algorithms.AES(key), modes.CBC(iv))
    encryptor = cipher.encryptor()
    return encryptor.update(data) + encryptor.finalize()
```

**C#:**
```csharp
using System.Security.Cryptography;

public static string Encrypt(string data, byte[] iv, byte[] key)
{
    using var aes = Aes.Create();
    aes.Key = key;
    aes.IV = iv;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    using var encryptor = aes.CreateEncryptor();
    var dataBytes = Encoding.UTF8.GetBytes(data);
    var encrypted = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
    
    return Convert.ToHexString(encrypted).ToLower();
}
```

### Exception Handling

**Python:**
```python
class PrivacyIDEAError(Exception):
    def __init__(self, description="error!", id=10):
        self.id = id
        self.message = description
        Exception.__init__(self, description)
```

**C#:**
```csharp
public class PrivacyIDEAError : Exception
{
    public int Id { get; set; }

    public PrivacyIDEAError(string description = "error!", int id = 10) 
        : base(description)
    {
        Id = id;
    }
}
```

## Project Structure

```
NetCore/PrivacyIdeaServer/
├── Controllers/           # API endpoints (Flask blueprints)
│   └── Api/              # API controllers
├── Lib/                  # Business logic (lib/)
│   ├── Crypto/          # Cryptography functions
│   ├── Tokens/          # Token management
│   ├── Resolvers/       # User resolvers
│   ├── Policies/        # Policy engine
│   ├── Audit/           # Audit logging
│   └── Security/        # Security utilities
├── Models/              # Database models
│   └── Database/        # EF Core entities
├── Services/            # Service layer
│   ├── Token/          # Token services
│   ├── Auth/           # Authentication services
│   └── User/           # User services
├── Program.cs           # Application entry point (app.py)
└── appsettings.json    # Configuration (pi.cfg)
```

## Configuration

### Python (pi.cfg):
```python
SQLALCHEMY_DATABASE_URI = 'sqlite:////etc/privacyidea/data.db'
SECRET_KEY = 'your-secret-key'
PI_PEPPER = "your-pepper"
```

### C# (appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/etc/privacyidea/data.db"
  },
  "AppSettings": {
    "SecretKey": "your-secret-key",
    "Pepper": "your-pepper"
  }
}
```

## Best Practices

### 1. Async/Await
- Use `async/await` throughout for I/O operations
- All database operations should be async
- All HTTP requests should be async

### 2. Dependency Injection
- Register services in `Program.cs`
- Inject dependencies via constructor
- Use interfaces for testability

### 3. Null Safety
- Use nullable reference types (`string?`)
- Enable nullable warnings in project file
- Validate parameters

### 4. Error Handling
- Use try-catch appropriately
- Log errors with ILogger
- Return appropriate HTTP status codes

### 5. Security
- Always hash passwords (BCrypt/Argon2)
- Use HTTPS only
- Validate all inputs
- Use parameterized queries (EF Core does this)
- Implement proper authentication/authorization

## Testing

### Python:
```python
def test_token_creation():
    token = Token("serial123", tokentype="HOTP")
    assert token.serial == "serial123"
```

### C#:
```csharp
[Fact]
public void TestTokenCreation()
{
    var token = new Token("serial123", tokenType: "HOTP");
    Assert.Equal("serial123", token.Serial);
}
```

## Migration Strategy

1. **Models First**: Convert all database models to ensure data layer is solid
2. **Core Library**: Convert business logic to ensure functionality is preserved
3. **API Layer**: Convert endpoints one-by-one, testing each
4. **Supporting Services**: Add SMS, email, RADIUS, etc. as needed
5. **Advanced Features**: FIDO2, CA connectors, etc.

## Progress Tracking

- **Total Python Files**: 361
- **Converted**: 26 (7.2%)
- **Database Models**: ~45 entities converted
- **Core Libraries**: Exception handling, basic crypto
- **Build Status**: ✅ SUCCESS

## Next Steps

1. Continue converting lib/ directory:
   - lib/token.py
   - lib/tokenclass.py
   - lib/user.py
   - lib/resolver.py
   - lib/policy/

2. Convert API controllers:
   - api/token.py
   - api/validate.py
   - api/auth.py

3. Implement services:
   - TokenService
   - AuthService
   - UserService

4. Add integration tests

## Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [PrivacyIDEA Documentation](https://privacyidea.readthedocs.io/)
