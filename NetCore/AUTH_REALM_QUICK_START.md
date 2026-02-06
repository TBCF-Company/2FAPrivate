# Quick Start - Authentication & Realm Management

## 🚀 Các File Đã Convert

✅ **Auth.cs** - Admin authentication
✅ **AuthCache.cs** - Auth caching với Argon2
✅ **Realm.cs** - Realm management
✅ **Resolver.cs** - Resolver CRUD operations
✅ **PasswordHasher.cs** - Password hashing utility

## 📦 Package Requirements

```bash
dotnet add package Konscious.Security.Cryptography.Argon2 --version 1.3.1
```

## ⚙️ Configuration

**appsettings.json:**
```json
{
  "PI_PEPPER": "your-secret-pepper-value",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=privacyidea.db"
  }
}
```

## 🔧 Dependency Injection Setup

**Program.cs hoặc Startup.cs:**
```csharp
// Initialize password hasher
PasswordHasher.Initialize(builder.Configuration);

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthCacheService>();
builder.Services.AddScoped<RealmService>();
builder.Services.AddScoped<ResolverService>();
builder.Services.AddScoped<ConfigService>();
```

## 💻 Usage Examples

### 1. Admin Authentication

```csharp
public class AdminController : ControllerBase
{
    private readonly AuthService _authService;
    
    public AdminController(AuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(string username, string password)
    {
        var isValid = await _authService.VerifyDbAdminAsync(username, password);
        
        if (isValid)
            return Ok(new { message = "Login successful" });
        else
            return Unauthorized(new { message = "Invalid credentials" });
    }
    
    [HttpPost("create")]
    public async Task<IActionResult> CreateAdmin(string username, string email, string password)
    {
        await _authService.CreateDbAdminAsync(username, email, password);
        return Ok(new { message = "Admin created successfully" });
    }
}
```

### 2. Authentication Caching

```csharp
public class AuthController : ControllerBase
{
    private readonly AuthCacheService _cacheService;
    
    [HttpPost("cache")]
    public async Task<IActionResult> CacheAuth(string username, string realm, string resolver, string password)
    {
        // Add to cache
        var cacheId = await _cacheService.AddToCacheAsync(username, realm, resolver, password);
        return Ok(new { cacheId });
    }
    
    [HttpPost("verify-cached")]
    public async Task<IActionResult> VerifyFromCache(string username, string realm, string resolver, string password)
    {
        // Verify from cache (fast path)
        var isValid = await _cacheService.VerifyInCacheAsync(
            username, realm, resolver, password, maxAuths: 10);
        
        if (isValid)
            return Ok(new { message = "Valid from cache" });
        else
            return Unauthorized(new { message = "Not in cache or expired" });
    }
}
```

### 3. Realm Management

```csharp
public class RealmController : ControllerBase
{
    private readonly RealmService _realmService;
    
    [HttpPost("create")]
    public async Task<IActionResult> CreateRealm(string name, List<ResolverInfo> resolvers)
    {
        var result = await _realmService.SetRealmAsync(name, resolvers);
        return Ok(new { 
            added = result.Added,
            failed = result.Failed 
        });
    }
    
    [HttpGet("{name}")]
    public async Task<IActionResult> GetRealm(string name)
    {
        var config = await _realmService.GetRealmAsync(name);
        return Ok(config);
    }
    
    [HttpPost("set-default")]
    public async Task<IActionResult> SetDefault(string name)
    {
        await _realmService.SetDefaultRealmAsync(name);
        return Ok(new { message = $"Realm '{name}' set as default" });
    }
}
```

### 4. Resolver Management

```csharp
public class ResolverController : ControllerBase
{
    private readonly ResolverService _resolverService;
    
    [HttpPost("create")]
    public async Task<IActionResult> CreateResolver([FromBody] Dictionary<string, object> parameters)
    {
        var resolverId = await _resolverService.SaveResolverAsync(parameters);
        return Ok(new { resolverId });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetResolvers(bool censor = true)
    {
        var resolvers = await _resolverService.GetResolverListAsync(censor: censor);
        return Ok(resolvers);
    }
    
    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteResolver(string name)
    {
        var id = await _resolverService.DeleteResolverAsync(name);
        
        if (id > 0)
            return Ok(new { message = $"Resolver '{name}' deleted" });
        else
            return NotFound(new { message = "Resolver not found" });
    }
}
```

## 🔒 Security Best Practices

### 1. Password Pepper Configuration
```bash
# Production: Use environment variable
export PI_PEPPER="$(openssl rand -base64 32)"

# Or Azure Key Vault
PI_PEPPER="@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/PIPepper/)"
```

### 2. Auth Cache Cleanup (Background Service)
```csharp
public class AuthCacheCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<AuthCacheService>();
            
            // Cleanup entries older than 60 minutes
            var deleted = await cacheService.CleanupAsync(60);
            _logger.LogInformation("Cleaned up {Count} expired auth cache entries", deleted);
            
            // Run every 10 minutes
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}

// Register in Program.cs
builder.Services.AddHostedService<AuthCacheCleanupService>();
```

## 🧪 Testing

### Unit Test Example
```csharp
[TestClass]
public class AuthServiceTests
{
    [TestMethod]
    public async Task VerifyDbAdmin_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PrivacyIDEAContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        
        using var context = new PrivacyIDEAContext(options);
        var logger = Mock.Of<ILogger<AuthService>>();
        var service = new AuthService(context, logger);
        
        // Create test admin
        await service.CreateDbAdminAsync("testadmin", null, "password123");
        
        // Act
        var result = await service.VerifyDbAdminAsync("testadmin", "password123");
        
        // Assert
        Assert.IsTrue(result);
    }
}
```

## 📊 Performance Tips

1. **Use AsNoTracking for read-only queries:**
```csharp
var admin = await _context.Admins
    .AsNoTracking()  // 20-30% faster
    .FirstOrDefaultAsync(a => a.Username == username);
```

2. **Bulk operations:**
```csharp
// Fast cleanup
await _context.AuthCaches
    .Where(ac => ac.LastAuth < cleanupTime)
    .ExecuteDeleteAsync();
```

3. **Eager loading:**
```csharp
var realms = await _context.Realms
    .Include(r => r.ResolverList)
        .ThenInclude(rr => rr.Resolver)
    .ToListAsync();
```

## ⚠️ Known Limitations

- ❌ Token library chưa implement → OTP authentication chưa hoạt động
- ❌ User library chưa implement → User password check chưa có
- ❌ Resolver implementations chưa có (LDAP, SQL, etc.)
- ❌ User cache service chưa implement
- ✅ Admin authentication: Working
- ✅ Auth caching: Working
- ✅ Realm CRUD: Working
- ✅ Resolver CRUD: Working (nhưng chờ resolver implementations)

## 📚 Full Documentation

Xem [AUTH_REALM_CONVERSION_REPORT.md](./AUTH_REALM_CONVERSION_REPORT.md) để biết chi tiết đầy đủ.

## 🛠️ Build & Run

```bash
cd NetCore/PrivacyIdeaServer

# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

**Build Status:** ✅ Build succeeded (0 errors, 10 warnings)

## 📞 Support

Nếu có vấn đề, kiểm tra:
1. Đã cài Argon2 package chưa?
2. PI_PEPPER đã config chưa?
3. Database connection string đúng chưa?
4. Services đã register trong DI container chưa?

---

**Version:** 1.0.0  
**Last Updated:** 2025-02-05
