# Hướng dẫn Khởi động Nhanh Resolver

## Tổng quan
Các resolver PrivacyIDEA cung cấp một giao diện thống nhất cho xác thực và quản lý người dùng trên nhiều kho người dùng khác nhau (LDAP, cơ sở dữ liệu SQL, HTTP API, v.v.).

## Các Resolver Có sẵn

### 1. PasswdIdResolver - Người dùng dựa trên File
Cho các file kiểu `/etc/passwd`.

```csharp
var resolver = new PasswdIdResolver(logger);
await resolver.LoadConfigAsync(new Dictionary<string, object>
{
    { "fileName", "/path/to/passwd/file" }
});
```

### 2. SqlIdResolver - Người dùng từ Cơ sở dữ liệu SQL
Cho PostgreSQL, MySQL, SQLite, SQL Server, v.v.

```csharp
var resolver = new SqlIdResolver(logger, dbContextFactory);
await resolver.LoadConfigAsync(new Dictionary<string, object>
{
    { "Driver", "postgresql" },
    { "Server", "localhost" },
    { "Database", "userdb" },
    { "User", "dbuser" },
    { "Password", "dbpass" },
    { "Table", "users" },
    { "Map", "username:login,userid:id,email:email" },
    { "Editable", true },
    { "Password_Hash_Type", "SSHA256" }
});
```

### 3. LdapIdResolver - Người dùng LDAP/Active Directory
Cho máy chủ LDAP và Active Directory.

```csharp
var resolver = new LdapIdResolver(logger);
await resolver.LoadConfigAsync(new Dictionary<string, object>
{
    { "LDAPURI", "ldaps://ldap.example.com:636" },
    { "LDAPBASE", "dc=example,dc=com" },
    { "BINDDN", "cn=admin,dc=example,dc=com" },
    { "BINDPW", "password" },
    { "UIDTYPE", "uid" },
    { "LOGINNAMEATTRIBUTE", "uid" },
    { "LDAPSEARCHFILTER", "(objectClass=inetOrgPerson)" },
    { "USERINFO", "username:uid,givenname:givenName,surname:sn,email:mail" }
});
```

### 4. HttpResolver - Người dùng từ REST API Chung
Class cơ sở cho các kho người dùng dựa trên HTTP.

```csharp
var resolver = new HttpResolver(logger, httpClientFactory);
await resolver.LoadConfigAsync(new Dictionary<string, object>
{
    { "base_url", "https://api.example.com" },
    { "username", "api_user" },
    { "password", "api_pass" },
    { "attribute_mapping", "username:login,userid:id,email:email" },
    { "config_get_user_by_id", new Dictionary<string, object>
        {
            { "method", "GET" },
            { "endpoint", "/users/{userid}" }
        }
    }
});
```

### 5. ScimIdResolver - Người dùng giao thức SCIM
Cho các kho người dùng tuân thủ SCIM.

```csharp
var resolver = new ScimIdResolver(logger, httpClientFactory);
await resolver.LoadConfigAsync(new Dictionary<string, object>
{
    { "authServer", "https://auth.example.com/oauth/token" },
    { "resourceServer", "https://api.example.com/scim/v2" },
    { "authClient", "client_id" },
    { "authSecret", "client_secret" }
});
```

### 6. EntraIdResolver - Microsoft Entra ID (Azure AD)
Cho Azure Active Directory / Microsoft 365.

```csharp
var resolver = new EntraIdResolver(logger, httpClientFactory);
await resolver.LoadConfigAsync(new Dictionary<string, object>
{
    { "client_id", "your-app-id" },
    { "tenant", "your-tenant-id" },
    { "client_secret", "your-client-secret" }
});
```

### 7. KeycloakResolver - Keycloak/Red Hat SSO
Cho Keycloak và Red Hat SSO.

```csharp
var resolver = new KeycloakResolver(logger, httpClientFactory);
await resolver.LoadConfigAsync(new Dictionary<string, object>
{
    { "base_url", "http://localhost:8080" },
    { "realm", "master" },
    { "username", "admin" },
    { "password", "admin_password" }
});
```

## Các Thao tác Thông dụng

### Xác thực một Người dùng
```csharp
var (success, username) = await resolver.CheckPassAsync("john.doe", "password123");
if (success)
{
    Console.WriteLine($"Người dùng {username} đã xác thực thành công");
}
```

### Lấy thông tin Người dùng
```csharp
var userInfo = await resolver.GetUserIdAsync("john.doe");
Console.WriteLine($"User ID: {userInfo}");

var userName = await resolver.GetUsernameAsync("12345");
Console.WriteLine($"Username: {userName}");
```

### Tìm kiếm Người dùng
```csharp
var users = await resolver.GetUserListAsync(new Dictionary<string, object>
{
    { "username", "*john*" }
});

foreach (var user in users)
{
    Console.WriteLine($"{user.Username} - {user.Email}");
}
```

### Cập nhật Người dùng (cho resolver có thể chỉnh sửa)
```csharp
var updated = await resolver.UpdateUserAsync(
    userId: "12345",
    attributes: new Dictionary<string, object>
    {
        { "email", "newemail@example.com" },
        { "phone", "+84123456789" }
    }
);
```

### Xóa Người dùng
```csharp
var deleted = await resolver.DeleteUserAsync("12345");
if (deleted)
{
    Console.WriteLine("Người dùng đã được xóa thành công");
}
```

## Cấu hình trong ASP.NET Core

### Đăng ký trong Program.cs
```csharp
// Đăng ký factory cho resolvers
builder.Services.AddSingleton<IResolverFactory, ResolverFactory>();

// Đăng ký các resolver cụ thể
builder.Services.AddTransient<PasswdIdResolver>();
builder.Services.AddTransient<SqlIdResolver>();
builder.Services.AddTransient<LdapIdResolver>();
builder.Services.AddTransient<HttpResolver>();
builder.Services.AddTransient<ScimIdResolver>();
builder.Services.AddTransient<EntraIdResolver>();
builder.Services.AddTransient<KeycloakResolver>();

// Đăng ký service quản lý
builder.Services.AddScoped<ResolverService>();
```

### Sử dụng trong Controller
```csharp
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IResolverFactory _resolverFactory;
    private readonly ResolverService _resolverService;
    
    public UserController(
        IResolverFactory resolverFactory,
        ResolverService resolverService)
    {
        _resolverFactory = resolverFactory;
        _resolverService = resolverService;
    }
    
    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate(
        string resolverName, 
        string username, 
        string password)
    {
        // Lấy cấu hình resolver
        var config = await _resolverService.GetResolverConfigAsync(resolverName);
        
        // Tạo resolver instance
        var resolver = _resolverFactory.CreateResolver(config.Type);
        await resolver.LoadConfigAsync(config.Data);
        
        // Xác thực
        var (success, user) = await resolver.CheckPassAsync(username, password);
        
        if (success)
            return Ok(new { message = "Xác thực thành công", user });
        else
            return Unauthorized(new { message = "Xác thực thất bại" });
    }
}
```

## Ánh xạ Thuộc tính

### Định dạng Ánh xạ
```
internal_name:external_name,internal_name2:external_name2
```

### Ví dụ cho SQL Resolver
```
username:login,userid:id,phone:mobile,email:mail,givenname:first_name,surname:last_name
```

### Ví dụ cho LDAP Resolver
```
username:uid,userid:uidNumber,email:mail,givenname:givenName,surname:sn,phone:telephoneNumber
```

## Các Thực hành Tốt nhất

### 1. Bảo mật Connection Strings
```csharp
// Sử dụng User Secrets cho development
// Sử dụng Azure Key Vault hoặc biến môi trường cho production
builder.Configuration.AddUserSecrets<Program>();
```

### 2. Connection Pooling
```csharp
// Đối với SQL resolvers, sử dụng connection pooling
services.AddDbContextPool<UserDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### 3. Caching
```csharp
// Cache kết quả resolver
services.AddMemoryCache();
services.AddScoped<CachedResolverService>();
```

### 4. Error Handling
```csharp
try
{
    var (success, user) = await resolver.CheckPassAsync(username, password);
    return success;
}
catch (ResolverNotAvailableError ex)
{
    _logger.LogError(ex, "Resolver không khả dụng");
    return false;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Lỗi không mong đợi trong quá trình xác thực");
    throw;
}
```

## Testing

### Unit Testing với Mock
```csharp
[TestClass]
public class ResolverTests
{
    [TestMethod]
    public async Task TestSqlResolver_Authenticate()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SqlIdResolver>>();
        var resolver = new SqlIdResolver(mockLogger.Object, dbContextFactory);
        
        await resolver.LoadConfigAsync(new Dictionary<string, object>
        {
            { "Driver", "sqlite" },
            { "Database", ":memory:" },
            // ... config
        });
        
        // Act
        var (success, user) = await resolver.CheckPassAsync("testuser", "password");
        
        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual("testuser", user);
    }
}
```

## Xử lý Sự cố

### Resolver không kết nối được
```csharp
// Kiểm tra logs
_logger.LogDebug("Đang kết nối tới {ResolverType} với config: {Config}", 
    resolverType, JsonSerializer.Serialize(config));

// Kiểm tra network connectivity
var canConnect = await NetworkUtilities.TestConnectionAsync(server, port);
```

### Xác thực thất bại
```csharp
// Bật verbose logging
await resolver.LoadConfigAsync(config);
resolver.LogLevel = LogLevel.Debug;

// Kiểm tra attribute mapping
var mappedAttrs = resolver.GetAttributeMapping();
_logger.LogDebug("Attribute mapping: {Mapping}", mappedAttrs);
```

### Hiệu suất kém
```csharp
// Sử dụng caching
services.AddDistributedMemoryCache();
services.AddScoped<ICachedResolver, CachedResolverWrapper>();

// Giới hạn kích thước kết quả search
var users = await resolver.GetUserListAsync(searchParams, limit: 100);
```

## Tài liệu Tham khảo

- `IIdResolver.cs` - Interface cơ bản cho tất cả resolvers
- `UserIdResolverBase.cs` - Class cơ sở với chức năng chung
- Tài liệu resolver cụ thể trong thư mục `Resolvers/`
