# Khởi động Nhanh - Quản lý Xác thực & Realm

## 🚀 Các File Đã Chuyển đổi

✅ **Auth.cs** - Xác thực Admin
✅ **AuthCache.cs** - Cache xác thực với Argon2
✅ **Realm.cs** - Quản lý Realm
✅ **Resolver.cs** - Các thao tác CRUD Resolver
✅ **PasswordHasher.cs** - Công cụ hash mật khẩu

## 📦 Yêu cầu Package

```bash
dotnet add package Konscious.Security.Cryptography.Argon2 --version 1.3.1
```

## ⚙️ Cấu hình

**appsettings.json:**
```json
{
  "PI_PEPPER": "giá-trị-pepper-bí-mật-của-bạn",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=privacyidea.db"
  }
}
```

## 🔧 Thiết lập Dependency Injection

**Program.cs hoặc Startup.cs:**
```csharp
// Khởi tạo password hasher
PasswordHasher.Initialize(builder.Configuration);

// Đăng ký services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthCacheService>();
builder.Services.AddScoped<RealmService>();
builder.Services.AddScoped<ResolverService>();
builder.Services.AddScoped<ConfigService>();
```

## 💻 Ví dụ Sử dụng

### 1. Xác thực Admin

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
            return Ok(new { message = "Đăng nhập thành công" });
        else
            return Unauthorized(new { message = "Thông tin xác thực không hợp lệ" });
    }
    
    [HttpPost("create")]
    public async Task<IActionResult> CreateAdmin(string username, string email, string password)
    {
        await _authService.CreateDbAdminAsync(username, email, password);
        return Ok(new { message = "Tạo Admin thành công" });
    }
}
```

### 2. Cache Xác thực

```csharp
public class AuthController : ControllerBase
{
    private readonly AuthCacheService _cacheService;
    
    [HttpPost("cache")]
    public async Task<IActionResult> CacheAuth(string username, string realm, string resolver, string password)
    {
        // Thêm vào cache
        var cacheId = await _cacheService.AddToCacheAsync(username, realm, resolver, password);
        return Ok(new { cacheId });
    }
    
    [HttpPost("verify-cached")]
    public async Task<IActionResult> VerifyFromCache(string username, string realm, string resolver, string password)
    {
        // Xác minh từ cache (đường dẫn nhanh)
        var isValid = await _cacheService.VerifyInCacheAsync(
            username, realm, resolver, password, maxAuths: 10);
        
        if (isValid)
            return Ok(new { message = "Xác thực hợp lệ từ cache" });
        else
            return Unauthorized(new { message = "Xác thực không hợp lệ" });
    }
}
```

### 3. Quản lý Realm

```csharp
public class RealmController : ControllerBase
{
    private readonly RealmService _realmService;
    
    [HttpPost]
    public async Task<IActionResult> CreateRealm(string realmName, List<string> resolverNames)
    {
        await _realmService.SetRealmAsync(realmName, resolverNames);
        return Ok(new { message = $"Realm '{realmName}' đã được tạo thành công" });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetRealms()
    {
        var realms = await _realmService.GetRealmsAsync();
        return Ok(realms);
    }
    
    [HttpGet("{realmName}")]
    public async Task<IActionResult> GetRealm(string realmName)
    {
        var realm = await _realmService.GetRealmAsync(realmName);
        if (realm != null)
            return Ok(realm);
        else
            return NotFound(new { message = "Không tìm thấy Realm" });
    }
    
    [HttpDelete("{realmName}")]
    public async Task<IActionResult> DeleteRealm(string realmName)
    {
        var result = await _realmService.DeleteRealmAsync(realmName);
        if (result > 0)
            return Ok(new { message = "Realm đã được xóa thành công" });
        else
            return NotFound(new { message = "Không tìm thấy Realm" });
    }
}
```

### 4. Quản lý Resolver

```csharp
public class ResolverController : ControllerBase
{
    private readonly ResolverService _resolverService;
    
    [HttpPost("{resolverName}")]
    public async Task<IActionResult> CreateResolver(
        string resolverName, 
        string resolverType,
        Dictionary<string, string> data)
    {
        var resolverId = await _resolverService.SaveResolverAsync(
            resolverName, 
            resolverType, 
            data);
        
        return Ok(new { 
            message = "Resolver đã được tạo thành công", 
            id = resolverId 
        });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetResolvers()
    {
        var resolvers = await _resolverService.GetResolversAsync();
        return Ok(resolvers);
    }
    
    [HttpGet("{resolverName}")]
    public async Task<IActionResult> GetResolver(string resolverName)
    {
        var resolver = await _resolverService.GetResolverConfigAsync(resolverName);
        if (resolver != null)
            return Ok(resolver);
        else
            return NotFound(new { message = "Không tìm thấy Resolver" });
    }
    
    [HttpDelete("{resolverName}")]
    public async Task<IActionResult> DeleteResolver(string resolverName)
    {
        var result = await _resolverService.DeleteResolverAsync(resolverName);
        if (result > 0)
            return Ok(new { message = "Resolver đã được xóa thành công" });
        else
            return NotFound(new { message = "Không tìm thấy Resolver" });
    }
}
```

## 🔐 Bảo mật

### Hashing Mật khẩu

Hệ thống sử dụng **Argon2id** để hash mật khẩu với các tham số sau:
- DegreeOfParallelism: 8
- MemorySize: 512 MB
- Iterations: 3

```csharp
// Hash mật khẩu
var hashedPassword = await PasswordHasher.HashPasswordAsync("myPassword123");

// Xác minh mật khẩu
var isValid = await PasswordHasher.VerifyPasswordAsync("myPassword123", hashedPassword);
```

### Pepper Configuration

Pepper là một giá trị bí mật được thêm vào tất cả các mật khẩu trước khi hash. Đặt nó trong `appsettings.json` hoặc biến môi trường:

```bash
export PI_PEPPER="your-secret-pepper-value-min-32-characters-long"
```

## 📊 Chi tiết Cơ sở dữ liệu

### Bảng Admin
- **id**: Primary key
- **username**: Tên đăng nhập duy nhất
- **email**: Địa chỉ email
- **password**: Mật khẩu đã hash (Argon2)

### Bảng Realm
- **id**: Primary key
- **name**: Tên realm duy nhất
- **default**: Boolean cho realm mặc định
- **option**: Các tùy chọn JSON

### Bảng Resolver
- **id**: Primary key
- **name**: Tên resolver duy nhất
- **rtype**: Loại (LDAP, SQL, SCIM, v.v.)
- **data**: Cấu hình JSON

### Bảng AuthCache
- **id**: Primary key
- **username**: Tên người dùng
- **realm**: Tên realm
- **resolver**: Tên resolver
- **authentication**: Giá trị xác thực đã hash
- **timestamp**: Thời gian tạo
- **first_auth**: Boolean cho lần xác thực đầu tiên

## 🧪 Testing

```csharp
[TestClass]
public class AuthServiceTests
{
    private AuthService _authService;
    
    [TestInitialize]
    public void Setup()
    {
        // Thiết lập in-memory database
        var options = new DbContextOptionsBuilder<PrivacyIDEAContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        
        var context = new PrivacyIDEAContext(options);
        _authService = new AuthService(context);
    }
    
    [TestMethod]
    public async Task TestCreateAndVerifyAdmin()
    {
        // Tạo admin
        await _authService.CreateDbAdminAsync("testadmin", "test@example.com", "password123");
        
        // Xác minh thông tin xác thực đúng
        var isValid = await _authService.VerifyDbAdminAsync("testadmin", "password123");
        Assert.IsTrue(isValid);
        
        // Xác minh thông tin xác thực sai
        var isInvalid = await _authService.VerifyDbAdminAsync("testadmin", "wrongpassword");
        Assert.IsFalse(isInvalid);
    }
}
```

## 🚀 Triển khai Production

### 1. Sử dụng Strong Pepper
```bash
export PI_PEPPER="$(openssl rand -base64 48)"
```

### 2. Bật HTTPS
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:5001"
      }
    }
  }
}
```

### 3. Cấu hình Rate Limiting
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
    });
});
```

### 4. Bật Logging
```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddFile("Logs/privacyidea-{Date}.txt");
});
```

## 📖 Tài liệu liên quan

- `Auth.cs` - Triển khai xác thực
- `AuthCache.cs` - Triển khai cache
- `Realm.cs` - Service quản lý Realm
- `Resolver.cs` - Service quản lý Resolver
- `PasswordHasher.cs` - Tiện ích hash mật khẩu
