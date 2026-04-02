using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Resolvers;

/// <summary>
/// Base class for all user resolvers
/// Maps to Python: privacyidea/lib/resolvers/UserIdResolver.py
/// </summary>
public abstract class ResolverBase : IUserResolver
{
    protected readonly ILogger Logger;
    protected Resolver? ResolverEntity;
    protected Dictionary<string, string> Config = new();

    protected ResolverBase(ILogger logger)
    {
        Logger = logger;
    }

    public abstract string Type { get; }
    public abstract string DisplayName { get; }

    public virtual void Initialize(Resolver resolver, Dictionary<string, string> config)
    {
        ResolverEntity = resolver;
        Config = config;
    }

    public virtual bool IsEditable => false;

    public abstract Task<ResolvedUser?> GetUserAsync(string userId);
    public abstract Task<IEnumerable<ResolvedUser>> SearchUsersAsync(string searchPattern, int maxResults = 100);
    public abstract Task<bool> CheckPasswordAsync(string userId, string password);
    public abstract Task<IEnumerable<string>> GetUserGroupsAsync(string userId);
    public abstract Task<Dictionary<string, string>> GetUserAttributesAsync(string userId);

    public virtual Task<string?> GetUserIdAsync(string username)
    {
        // Default: username is userId
        return Task.FromResult<string?>(username);
    }

    public virtual async Task<IEnumerable<UserInfo>> GetUsersAsync(Dictionary<string, string> searchParams)
    {
        var pattern = searchParams.GetValueOrDefault("username", "*");
        var resolved = await SearchUsersAsync(pattern);
        return resolved.Select(r => new UserInfo
        {
            Username = r.UserName,
            UserId = r.UserId,
            Email = r.Email,
            GivenName = r.GivenName,
            Surname = r.Surname,
            Phone = r.Phone,
            Mobile = r.Mobile,
            Description = r.Description,
            Attributes = r.Attributes,
            Editable = IsEditable
        });
    }

    public virtual Task<string> AddUserAsync(Dictionary<string, string> attributes)
    {
        throw new NotSupportedException("This resolver does not support adding users");
    }

    public virtual Task<bool> UpdateUserAsync(string userId, Dictionary<string, string> attributes)
    {
        throw new NotSupportedException("This resolver does not support updating users");
    }

    public virtual Task<bool> DeleteUserAsync(string userId)
    {
        throw new NotSupportedException("This resolver does not support deleting users");
    }

    public virtual Task<bool> TestConnectionAsync()
    {
        return Task.FromResult(true);
    }

    public virtual Task<int> GetUserCountAsync()
    {
        return Task.FromResult(0);
    }

    protected string GetConfigValue(string key, string defaultValue = "")
    {
        return Config.TryGetValue(key, out var value) ? value : defaultValue;
    }

    protected int GetConfigInt(string key, int defaultValue = 0)
    {
        return Config.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : defaultValue;
    }

    protected bool GetConfigBool(string key, bool defaultValue = false)
    {
        return Config.TryGetValue(key, out var value) && bool.TryParse(value, out var result) ? result : defaultValue;
    }
}

/// <summary>
/// LDAP User Resolver
/// Maps to Python: privacyidea/lib/resolvers/LDAPIdResolver.py
/// </summary>
public class LdapResolver : ResolverBase
{
    public override string Type => "ldap";
    public override string DisplayName => "LDAP Resolver";

    // LDAP configuration
    private string _ldapUri = "";
    private string _baseDn = "";
    private string _bindDn = "";
    private string _bindPassword = "";
    private string _userFilter = "(objectClass=person)";
    private string _uidAttribute = "uid";
    private string _loginAttribute = "uid";
    private bool _useTls;
    private int _timeout = 30;
    private Dictionary<string, string> _attributeMapping = new();

    public LdapResolver(ILogger<LdapResolver> logger) : base(logger)
    {
    }

    public override void Initialize(Resolver resolver, Dictionary<string, string> config)
    {
        base.Initialize(resolver, config);

        _ldapUri = GetConfigValue("LDAPURI");
        _baseDn = GetConfigValue("LDAPBASE");
        _bindDn = GetConfigValue("BINDDN");
        _bindPassword = GetConfigValue("BINDPW");
        _userFilter = GetConfigValue("LDAPFILTER", "(objectClass=person)");
        _uidAttribute = GetConfigValue("UIDTYPE", "uid");
        _loginAttribute = GetConfigValue("LOGINNAMEATTRIBUTE", "uid");
        _useTls = GetConfigBool("STARTTLS", false);
        _timeout = GetConfigInt("TIMEOUT", 30);

        // Parse attribute mapping
        var mapping = GetConfigValue("USERINFO", "{}");
        try
        {
            _attributeMapping = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(mapping)
                                ?? new Dictionary<string, string>();
        }
        catch
        {
            _attributeMapping = new Dictionary<string, string>();
        }
    }

    public override async Task<ResolvedUser?> GetUserAsync(string userId)
    {
        try
        {
            // Using Novell.Directory.Ldap
            using var connection = CreateConnection();
            await Task.Run(() => connection.Bind(_bindDn, _bindPassword));

            var filter = $"(&{_userFilter}({_loginAttribute}={EscapeLdap(userId)}))";
            var searchResults = connection.Search(
                _baseDn,
                Novell.Directory.Ldap.LdapConnection.ScopeSub,
                filter,
                null,
                false);

            if (searchResults.HasMore())
            {
                var entry = searchResults.Next();
                return MapEntryToUser(entry);
            }

            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting user {UserId} from LDAP", userId);
            return null;
        }
    }

    public override async Task<IEnumerable<ResolvedUser>> SearchUsersAsync(string searchPattern, int maxResults = 100)
    {
        var users = new List<ResolvedUser>();

        try
        {
            using var connection = CreateConnection();
            await Task.Run(() => connection.Bind(_bindDn, _bindPassword));

            var filter = $"(&{_userFilter}({_loginAttribute}=*{EscapeLdap(searchPattern)}*))";
            var searchResults = connection.Search(
                _baseDn,
                Novell.Directory.Ldap.LdapConnection.ScopeSub,
                filter,
                null,
                false);

            var count = 0;
            while (searchResults.HasMore() && count < maxResults)
            {
                try
                {
                    var entry = searchResults.Next();
                    var user = MapEntryToUser(entry);
                    if (user != null)
                    {
                        users.Add(user);
                        count++;
                    }
                }
                catch (Novell.Directory.Ldap.LdapException)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching users in LDAP with pattern {Pattern}", searchPattern);
        }

        return users;
    }

    public override async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        try
        {
            // First, find the user DN
            var user = await GetUserAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.Dn))
                return false;

            // Try to bind with user's credentials
            using var connection = CreateConnection();
            try
            {
                await Task.Run(() => connection.Bind(user.Dn, password));
                return true;
            }
            catch
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking password for user {UserId}", userId);
            return false;
        }
    }

    public override async Task<IEnumerable<string>> GetUserGroupsAsync(string userId)
    {
        var groups = new List<string>();

        try
        {
            var user = await GetUserAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.Dn))
                return groups;

            using var connection = CreateConnection();
            await Task.Run(() => connection.Bind(_bindDn, _bindPassword));

            // Search for groups containing this user
            var groupFilter = GetConfigValue("LDAPGROUPFILTER", "(objectClass=groupOfNames)");
            var memberAttr = GetConfigValue("LDAPGROUPMEMBERATTR", "member");
            var filter = $"(&{groupFilter}({memberAttr}={EscapeLdap(user.Dn)}))";

            var searchResults = connection.Search(
                _baseDn,
                Novell.Directory.Ldap.LdapConnection.ScopeSub,
                filter,
                new[] { "cn" },
                false);

            while (searchResults.HasMore())
            {
                try
                {
                    var entry = searchResults.Next();
                    var cn = entry.GetAttribute("cn")?.StringValue;
                    if (!string.IsNullOrEmpty(cn))
                    {
                        groups.Add(cn);
                    }
                }
                catch (Novell.Directory.Ldap.LdapException)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting groups for user {UserId}", userId);
        }

        return groups;
    }

    public override async Task<Dictionary<string, string>> GetUserAttributesAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        return user?.Attributes ?? new Dictionary<string, string>();
    }

    public override async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var connection = CreateConnection();
            await Task.Run(() => connection.Bind(_bindDn, _bindPassword));
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "LDAP connection test failed");
            return false;
        }
    }

    public override async Task<int> GetUserCountAsync()
    {
        try
        {
            using var connection = CreateConnection();
            await Task.Run(() => connection.Bind(_bindDn, _bindPassword));

            var searchResults = connection.Search(
                _baseDn,
                Novell.Directory.Ldap.LdapConnection.ScopeSub,
                _userFilter,
                new[] { _uidAttribute },
                false);

            var count = 0;
            while (searchResults.HasMore())
            {
                try
                {
                    searchResults.Next();
                    count++;
                }
                catch (Novell.Directory.Ldap.LdapException)
                {
                    break;
                }
            }
            return count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting user count from LDAP");
            return 0;
        }
    }

    private Novell.Directory.Ldap.LdapConnection CreateConnection()
    {
        var connection = new Novell.Directory.Ldap.LdapConnection();
        
        // Parse URI
        var uri = new Uri(_ldapUri);
        var port = uri.Port > 0 ? uri.Port : (uri.Scheme == "ldaps" ? 636 : 389);

        if (uri.Scheme == "ldaps")
        {
            // Use SSL
            connection.SecureSocketLayer = true;
        }

        connection.Connect(uri.Host, port);

        if (_useTls && !connection.SecureSocketLayer)
        {
            connection.StartTls();
        }

        return connection;
    }

    private ResolvedUser? MapEntryToUser(Novell.Directory.Ldap.LdapEntry entry)
    {
        var user = new ResolvedUser
        {
            UserId = entry.GetAttribute(_uidAttribute)?.StringValue ?? "",
            UserName = entry.GetAttribute(_loginAttribute)?.StringValue ?? "",
            Dn = entry.Dn,
            ResolverName = ResolverEntity?.Name ?? "",
            Attributes = new Dictionary<string, string>()
        };

        if (string.IsNullOrEmpty(user.UserId))
            return null;

        // Map configured attributes
        foreach (var (key, ldapAttr) in _attributeMapping)
        {
            var attr = entry.GetAttribute(ldapAttr);
            if (attr != null)
            {
                user.Attributes[key] = attr.StringValue;
            }
        }

        // Common attributes
        user.Email = entry.GetAttribute("mail")?.StringValue;
        user.GivenName = entry.GetAttribute("givenName")?.StringValue;
        user.Surname = entry.GetAttribute("sn")?.StringValue;
        user.Phone = entry.GetAttribute("telephoneNumber")?.StringValue;
        user.Mobile = entry.GetAttribute("mobile")?.StringValue;

        return user;
    }

    private static string EscapeLdap(string input)
    {
        // Escape special LDAP filter characters
        return input
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
    }
}

/// <summary>
/// SQL User Resolver
/// Maps to Python: privacyidea/lib/resolvers/SQLIdResolver.py
/// </summary>
public class SqlResolver : ResolverBase
{
    public override string Type => "sql";
    public override string DisplayName => "SQL Resolver";

    private string _connectionString = "";
    private string _tableName = "";
    private string _userIdColumn = "id";
    private string _userNameColumn = "username";
    private string _passwordColumn = "password";
    private string _passwordHashType = "sha256";
    private Dictionary<string, string> _columnMapping = new();

    public SqlResolver(ILogger<SqlResolver> logger) : base(logger)
    {
    }

    public override void Initialize(Resolver resolver, Dictionary<string, string> config)
    {
        base.Initialize(resolver, config);

        var driver = GetConfigValue("Driver", "mysql");
        var server = GetConfigValue("Server");
        var database = GetConfigValue("Database");
        var user = GetConfigValue("User");
        var password = GetConfigValue("Password");
        var port = GetConfigInt("Port", driver == "mysql" ? 3306 : 5432);

        _connectionString = driver.ToLower() switch
        {
            "mysql" => $"Server={server};Port={port};Database={database};User={user};Password={password};",
            "postgresql" => $"Host={server};Port={port};Database={database};Username={user};Password={password};",
            "sqlite" => $"Data Source={database};",
            "mssql" => $"Server={server},{port};Database={database};User Id={user};Password={password};",
            _ => throw new NotSupportedException($"Database driver {driver} not supported")
        };

        _tableName = GetConfigValue("Table", "users");
        _userIdColumn = GetConfigValue("uidColumn", "id");
        _userNameColumn = GetConfigValue("loginname", "username");
        _passwordColumn = GetConfigValue("password", "password");
        _passwordHashType = GetConfigValue("HashType", "sha256");

        // Parse column mapping
        var mapping = GetConfigValue("Map", "{}");
        try
        {
            _columnMapping = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(mapping)
                             ?? new Dictionary<string, string>();
        }
        catch
        {
            _columnMapping = new Dictionary<string, string>();
        }
    }

    public override async Task<ResolvedUser?> GetUserAsync(string userId)
    {
        // This is a simplified implementation
        // In production, use proper database-specific connectors
        Logger.LogInformation("Getting user {UserId} from SQL resolver", userId);
        
        // Placeholder - actual implementation would use ADO.NET with proper parameterization
        await Task.CompletedTask;
        
        // Return mock for now
        return null;
    }

    public override async Task<IEnumerable<ResolvedUser>> SearchUsersAsync(string searchPattern, int maxResults = 100)
    {
        Logger.LogInformation("Searching users in SQL resolver with pattern {Pattern}", searchPattern);
        await Task.CompletedTask;
        return Array.Empty<ResolvedUser>();
    }

    public override async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        Logger.LogInformation("Checking password for user {UserId} in SQL resolver", userId);
        await Task.CompletedTask;
        return false;
    }

    public override Task<IEnumerable<string>> GetUserGroupsAsync(string userId)
    {
        // SQL resolver typically doesn't support groups
        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }

    public override async Task<Dictionary<string, string>> GetUserAttributesAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        return user?.Attributes ?? new Dictionary<string, string>();
    }
}

/// <summary>
/// EntraID (Azure AD) Resolver
/// Maps to Python: privacyidea/lib/resolvers/EntraIdResolver.py
/// </summary>
public class EntraIdResolver : ResolverBase
{
    public override string Type => "entraid";
    public override string DisplayName => "Microsoft Entra ID Resolver";

    private string _tenantId = "";
    private string _clientId = "";
    private string _clientSecret = "";
    private string _graphApiUrl = "https://graph.microsoft.com/v1.0";

    private readonly HttpClient _httpClient;

    public EntraIdResolver(ILogger<EntraIdResolver> logger, HttpClient? httpClient = null) : base(logger)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public override void Initialize(Resolver resolver, Dictionary<string, string> config)
    {
        base.Initialize(resolver, config);

        _tenantId = GetConfigValue("TenantId");
        _clientId = GetConfigValue("ClientId");
        _clientSecret = GetConfigValue("ClientSecret");
        _graphApiUrl = GetConfigValue("GraphApiUrl", "https://graph.microsoft.com/v1.0");
    }

    public override async Task<ResolvedUser?> GetUserAsync(string userId)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
                return null;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // Try by UPN first, then by ID
            var response = await _httpClient.GetAsync($"{_graphApiUrl}/users/{Uri.EscapeDataString(userId)}");
            
            if (!response.IsSuccessStatusCode)
            {
                // Try searching
                response = await _httpClient.GetAsync(
                    $"{_graphApiUrl}/users?$filter=mail eq '{Uri.EscapeDataString(userId)}' or userPrincipalName eq '{Uri.EscapeDataString(userId)}'");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var searchResult = await response.Content.ReadAsStringAsync();
                var searchData = System.Text.Json.JsonDocument.Parse(searchResult);
                var users = searchData.RootElement.GetProperty("value");
                
                if (users.GetArrayLength() == 0)
                    return null;

                return MapJsonToUser(users[0]);
            }

            var content = await response.Content.ReadAsStringAsync();
            var userData = System.Text.Json.JsonDocument.Parse(content);
            return MapJsonToUser(userData.RootElement);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting user {UserId} from Entra ID", userId);
            return null;
        }
    }

    public override async Task<IEnumerable<ResolvedUser>> SearchUsersAsync(string searchPattern, int maxResults = 100)
    {
        var users = new List<ResolvedUser>();

        try
        {
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
                return users;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var filter = Uri.EscapeDataString($"startswith(displayName,'{searchPattern}') or startswith(mail,'{searchPattern}')");
            var response = await _httpClient.GetAsync($"{_graphApiUrl}/users?$filter={filter}&$top={maxResults}");
            
            if (!response.IsSuccessStatusCode)
                return users;

            var content = await response.Content.ReadAsStringAsync();
            var data = System.Text.Json.JsonDocument.Parse(content);
            
            foreach (var userJson in data.RootElement.GetProperty("value").EnumerateArray())
            {
                var user = MapJsonToUser(userJson);
                if (user != null)
                    users.Add(user);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching users in Entra ID");
        }

        return users;
    }

    public override async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        // Entra ID password validation requires OAuth Resource Owner Password Credentials flow
        // This is only available for certain configurations
        try
        {
            var tokenUrl = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["scope"] = "https://graph.microsoft.com/.default",
                ["username"] = userId,
                ["password"] = password
            });

            var response = await _httpClient.PostAsync(tokenUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking password for user {UserId} in Entra ID", userId);
            return false;
        }
    }

    public override async Task<IEnumerable<string>> GetUserGroupsAsync(string userId)
    {
        var groups = new List<string>();

        try
        {
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
                return groups;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{_graphApiUrl}/users/{Uri.EscapeDataString(userId)}/memberOf");
            
            if (!response.IsSuccessStatusCode)
                return groups;

            var content = await response.Content.ReadAsStringAsync();
            var data = System.Text.Json.JsonDocument.Parse(content);
            
            foreach (var group in data.RootElement.GetProperty("value").EnumerateArray())
            {
                if (group.TryGetProperty("displayName", out var displayName))
                {
                    groups.Add(displayName.GetString() ?? "");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting groups for user {UserId} in Entra ID", userId);
        }

        return groups;
    }

    public override async Task<Dictionary<string, string>> GetUserAttributesAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        return user?.Attributes ?? new Dictionary<string, string>();
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var tokenUrl = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["scope"] = "https://graph.microsoft.com/.default"
            });

            var response = await _httpClient.PostAsync(tokenUrl, content);
            if (!response.IsSuccessStatusCode)
                return null;

            var responseBody = await response.Content.ReadAsStringAsync();
            var tokenData = System.Text.Json.JsonDocument.Parse(responseBody);
            return tokenData.RootElement.GetProperty("access_token").GetString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting Entra ID access token");
            return null;
        }
    }

    private ResolvedUser? MapJsonToUser(System.Text.Json.JsonElement json)
    {
        try
        {
            var user = new ResolvedUser
            {
                UserId = json.GetProperty("id").GetString() ?? "",
                UserName = json.TryGetProperty("userPrincipalName", out var upn) ? upn.GetString() ?? "" : "",
                Email = json.TryGetProperty("mail", out var mail) ? mail.GetString() : null,
                GivenName = json.TryGetProperty("givenName", out var gn) ? gn.GetString() : null,
                Surname = json.TryGetProperty("surname", out var sn) ? sn.GetString() : null,
                Phone = json.TryGetProperty("businessPhones", out var phones) && phones.GetArrayLength() > 0 
                    ? phones[0].GetString() : null,
                Mobile = json.TryGetProperty("mobilePhone", out var mobile) ? mobile.GetString() : null,
                ResolverName = ResolverEntity?.Name ?? "",
                Attributes = new Dictionary<string, string>()
            };

            // Add all properties as attributes
            foreach (var prop in json.EnumerateObject())
            {
                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    user.Attributes[prop.Name] = prop.Value.GetString() ?? "";
                }
            }

            return user;
        }
        catch
        {
            return null;
        }
    }
}
