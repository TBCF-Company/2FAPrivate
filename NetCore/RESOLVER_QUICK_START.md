# Resolver Quick Start Guide

## Overview
The PrivacyIDEA resolvers provide a unified interface for user authentication and management across various user stores (LDAP, SQL databases, HTTP APIs, etc.).

## Available Resolvers

### 1. PasswdIdResolver - File-based Users
For `/etc/passwd` style files.

```csharp
var resolver = new PasswdIdResolver(logger);
await resolver.LoadConfigAsync(new Dictionary<string, object>
{
    { "fileName", "/path/to/passwd/file" }
});
```

### 2. SqlIdResolver - SQL Database Users
For PostgreSQL, MySQL, SQLite, SQL Server, etc.

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

### 3. LdapIdResolver - LDAP/Active Directory Users
For LDAP servers and Active Directory.

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

### 4. HttpResolver - Generic REST API Users
Base class for HTTP-based user stores.

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

### 5. ScimIdResolver - SCIM Protocol Users
For SCIM-compliant user stores.

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
For Azure Active Directory / Microsoft 365.

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
For Keycloak and Red Hat SSO.

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

## Common Operations

### Authenticate a User
```csharp
var userId = await resolver.GetUserIdAsync(username);
if (!string.IsNullOrEmpty(userId))
{
    bool isValid = await resolver.CheckPassAsync(userId, password);
    if (isValid)
    {
        // User authenticated successfully
    }
}
```

### Get User Information
```csharp
var userId = await resolver.GetUserIdAsync(username);
var userInfo = await resolver.GetUserInfoAsync(userId);

// userInfo contains:
// - username
// - userid
// - email
// - givenname
// - surname
// - phone
// - mobile
// etc.
```

### Search for Users
```csharp
var searchCriteria = new Dictionary<string, string>
{
    { "email", "*@example.com" },
    { "givenname", "John*" }
};

var users = await resolver.GetUserListAsync(searchCriteria);
foreach (var user in users)
{
    Console.WriteLine($"User: {user["username"]}, Email: {user["email"]}");
}
```

### Create a User (Editable Resolvers Only)
```csharp
var attributes = new Dictionary<string, object>
{
    { "username", "johndoe" },
    { "password", "SecurePass123!" },
    { "email", "john@example.com" },
    { "givenname", "John" },
    { "surname", "Doe" }
};

var newUserId = await resolver.AddUserAsync(attributes);
if (newUserId != null)
{
    // User created successfully
}
```

### Update a User (Editable Resolvers Only)
```csharp
var updates = new Dictionary<string, object>
{
    { "email", "newemail@example.com" },
    { "mobile", "+1234567890" }
};

bool success = await resolver.UpdateUserAsync(userId, updates);
```

### Delete a User (Editable Resolvers Only)
```csharp
bool success = await resolver.DeleteUserAsync(userId);
```

## Dependency Injection Setup

### Register in Program.cs or Startup.cs
```csharp
// Register resolvers
services.AddScoped<PasswdIdResolver>();
services.AddScoped<LdapIdResolver>();
services.AddScoped<SqlIdResolver>();
services.AddHttpClient(); // Required for HTTP-based resolvers

// Register factory for creating resolvers dynamically
services.AddSingleton<IResolverFactory, ResolverFactory>();
```

### Example Resolver Factory
```csharp
public interface IResolverFactory
{
    IUserIdResolver CreateResolver(string type);
}

public class ResolverFactory : IResolverFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ResolverFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IUserIdResolver CreateResolver(string type)
    {
        return type.ToLower() switch
        {
            "passwd" => _serviceProvider.GetRequiredService<PasswdIdResolver>(),
            "sql" => _serviceProvider.GetRequiredService<SqlIdResolver>(),
            "ldap" => _serviceProvider.GetRequiredService<LdapIdResolver>(),
            "http" => _serviceProvider.GetRequiredService<HttpResolver>(),
            "scim" => _serviceProvider.GetRequiredService<ScimIdResolver>(),
            "entraid" => _serviceProvider.GetRequiredService<EntraIdResolver>(),
            "keycloak" => _serviceProvider.GetRequiredService<KeycloakResolver>(),
            _ => throw new ArgumentException($"Unknown resolver type: {type}")
        };
    }
}
```

## Error Handling

All resolver methods use async/await and can throw exceptions. Always wrap calls in try-catch:

```csharp
try
{
    var userId = await resolver.GetUserIdAsync(username);
    var isValid = await resolver.CheckPassAsync(userId, password);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error authenticating user {Username}", username);
    // Handle error appropriately
}
```

## Performance Tips

1. **Caching**: LDAP resolver implements caching. Configure cache timeout:
   ```csharp
   { "CACHE_TIMEOUT", 300 } // 5 minutes
   ```

2. **Connection Pooling**: SQL resolver uses EF Core connection pooling automatically.

3. **Timeouts**: Configure appropriate timeouts for network operations:
   ```csharp
   { "TIMEOUT", 30 } // 30 seconds for HTTP
   { "timeout", 10 } // 10 seconds for LDAP
   ```

4. **Size Limits**: Limit search results to avoid memory issues:
   ```csharp
   { "SIZELIMIT", 500 } // LDAP
   { "Limit", 100 } // SQL
   ```

## Security Considerations

1. **Store credentials securely**: Use Azure Key Vault, AWS Secrets Manager, or similar.
2. **Use LDAPS/TLS**: Always use encrypted connections.
3. **Validate input**: Sanitize user input before passing to resolvers.
4. **Log carefully**: Don't log passwords or sensitive data.
5. **Use strong password hashing**: Prefer BCrypt or SSHA256 for SQL resolver.

## Testing

```csharp
[Fact]
public async Task TestUserAuthentication()
{
    // Arrange
    var logger = Mock.Of<ILogger<LdapIdResolver>>();
    var resolver = new LdapIdResolver(logger);
    await resolver.LoadConfigAsync(testConfig);

    // Act
    var userId = await resolver.GetUserIdAsync("testuser");
    var isValid = await resolver.CheckPassAsync(userId, "password");

    // Assert
    Assert.True(isValid);
}
```

## Support

For issues or questions:
- Check the detailed report: `NetCore/RESOLVER_CONVERSION_REPORT.md`
- Review XML documentation in code
- See Python original implementation in `privacyidea/privacyidea/lib/resolvers/`
