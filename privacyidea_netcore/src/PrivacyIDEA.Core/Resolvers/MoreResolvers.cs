using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Resolvers;

/// <summary>
/// SCIM User Resolver
/// Maps to Python: privacyidea/lib/resolvers/SCIMIdResolver.py
/// System for Cross-domain Identity Management
/// </summary>
public class ScimResolver : ResolverBase
{
    public override string Type => "scim";
    public override string DisplayName => "SCIM Resolver";

    private string _baseUrl = "";
    private string _authToken = "";
    private readonly HttpClient _httpClient;

    public ScimResolver(ILogger<ScimResolver> logger, HttpClient? httpClient = null) : base(logger)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public override void Initialize(Resolver resolver, Dictionary<string, string> config)
    {
        base.Initialize(resolver, config);

        _baseUrl = GetConfigValue("Endpoint").TrimEnd('/');
        _authToken = GetConfigValue("AuthToken");
    }

    public override async Task<ResolvedUser?> GetUserAsync(string userId)
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.GetAsync($"{_baseUrl}/Users/{Uri.EscapeDataString(userId)}");
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var userData = System.Text.Json.JsonDocument.Parse(content);
            return MapScimUserToResolvedUser(userData.RootElement);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting user {UserId} from SCIM", userId);
            return null;
        }
    }

    public override async Task<IEnumerable<ResolvedUser>> SearchUsersAsync(string searchPattern, int maxResults = 100)
    {
        var users = new List<ResolvedUser>();

        try
        {
            SetAuthHeader();
            var filter = Uri.EscapeDataString($"userName co \"{searchPattern}\" or displayName co \"{searchPattern}\"");
            var response = await _httpClient.GetAsync($"{_baseUrl}/Users?filter={filter}&count={maxResults}");
            
            if (!response.IsSuccessStatusCode)
                return users;

            var content = await response.Content.ReadAsStringAsync();
            var data = System.Text.Json.JsonDocument.Parse(content);
            
            if (data.RootElement.TryGetProperty("Resources", out var resources))
            {
                foreach (var userJson in resources.EnumerateArray())
                {
                    var user = MapScimUserToResolvedUser(userJson);
                    if (user != null)
                        users.Add(user);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching users in SCIM");
        }

        return users;
    }

    public override Task<bool> CheckPasswordAsync(string userId, string password)
    {
        // SCIM doesn't typically support password verification
        Logger.LogWarning("SCIM resolver does not support password verification");
        return Task.FromResult(false);
    }

    public override async Task<IEnumerable<string>> GetUserGroupsAsync(string userId)
    {
        var groups = new List<string>();

        try
        {
            SetAuthHeader();
            var filter = Uri.EscapeDataString($"members.value eq \"{userId}\"");
            var response = await _httpClient.GetAsync($"{_baseUrl}/Groups?filter={filter}");
            
            if (!response.IsSuccessStatusCode)
                return groups;

            var content = await response.Content.ReadAsStringAsync();
            var data = System.Text.Json.JsonDocument.Parse(content);
            
            if (data.RootElement.TryGetProperty("Resources", out var resources))
            {
                foreach (var group in resources.EnumerateArray())
                {
                    if (group.TryGetProperty("displayName", out var displayName))
                    {
                        groups.Add(displayName.GetString() ?? "");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting groups for user {UserId} in SCIM", userId);
        }

        return groups;
    }

    public override async Task<Dictionary<string, string>> GetUserAttributesAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        return user?.Attributes ?? new Dictionary<string, string>();
    }

    private void SetAuthHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
    }

    private ResolvedUser? MapScimUserToResolvedUser(System.Text.Json.JsonElement json)
    {
        try
        {
            var user = new ResolvedUser
            {
                UserId = json.GetProperty("id").GetString() ?? "",
                UserName = json.TryGetProperty("userName", out var un) ? un.GetString() ?? "" : "",
                ResolverName = ResolverEntity?.Name ?? "",
                Attributes = new Dictionary<string, string>()
            };

            if (json.TryGetProperty("name", out var name))
            {
                user.GivenName = name.TryGetProperty("givenName", out var gn) ? gn.GetString() : null;
                user.Surname = name.TryGetProperty("familyName", out var fn) ? fn.GetString() : null;
            }

            if (json.TryGetProperty("emails", out var emails))
            {
                foreach (var email in emails.EnumerateArray())
                {
                    if (email.TryGetProperty("primary", out var primary) && primary.GetBoolean())
                    {
                        user.Email = email.TryGetProperty("value", out var val) ? val.GetString() : null;
                        break;
                    }
                }
                // Fallback to first email
                if (string.IsNullOrEmpty(user.Email) && emails.GetArrayLength() > 0)
                {
                    user.Email = emails[0].TryGetProperty("value", out var val) ? val.GetString() : null;
                }
            }

            if (json.TryGetProperty("phoneNumbers", out var phones))
            {
                foreach (var phone in phones.EnumerateArray())
                {
                    var type = phone.TryGetProperty("type", out var t) ? t.GetString() : "";
                    var value = phone.TryGetProperty("value", out var v) ? v.GetString() : null;
                    
                    if (type == "mobile")
                        user.Mobile = value;
                    else if (type == "work" || string.IsNullOrEmpty(user.Phone))
                        user.Phone = value;
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

/// <summary>
/// HTTP/REST User Resolver
/// Maps to Python: privacyidea/lib/resolvers/HTTPResolver.py
/// Generic HTTP API resolver
/// </summary>
public class HttpResolver : ResolverBase
{
    public override string Type => "http";
    public override string DisplayName => "HTTP Resolver";

    private string _endpoint = "";
    private string _getUserPath = "";
    private string _searchUsersPath = "";
    private string _checkPasswordPath = "";
    private Dictionary<string, string> _headers = new();
    private readonly HttpClient _httpClient;

    public HttpResolver(ILogger<HttpResolver> logger, HttpClient? httpClient = null) : base(logger)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public override void Initialize(Resolver resolver, Dictionary<string, string> config)
    {
        base.Initialize(resolver, config);

        _endpoint = GetConfigValue("ENDPOINT").TrimEnd('/');
        _getUserPath = GetConfigValue("GET_USER_PATH", "/user/{userid}");
        _searchUsersPath = GetConfigValue("SEARCH_USERS_PATH", "/users?search={pattern}");
        _checkPasswordPath = GetConfigValue("CHECK_PASSWORD_PATH", "/auth");

        // Parse headers
        var headersJson = GetConfigValue("HEADERS", "{}");
        try
        {
            _headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson)
                       ?? new Dictionary<string, string>();
        }
        catch
        {
            _headers = new Dictionary<string, string>();
        }
    }

    public override async Task<ResolvedUser?> GetUserAsync(string userId)
    {
        try
        {
            var path = _getUserPath.Replace("{userid}", Uri.EscapeDataString(userId));
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_endpoint}{path}");
            AddHeaders(request);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var userData = System.Text.Json.JsonDocument.Parse(content);
            return MapHttpResponseToUser(userData.RootElement);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting user {UserId} from HTTP resolver", userId);
            return null;
        }
    }

    public override async Task<IEnumerable<ResolvedUser>> SearchUsersAsync(string searchPattern, int maxResults = 100)
    {
        var users = new List<ResolvedUser>();

        try
        {
            var path = _searchUsersPath
                .Replace("{pattern}", Uri.EscapeDataString(searchPattern))
                .Replace("{limit}", maxResults.ToString());
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_endpoint}{path}");
            AddHeaders(request);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
                return users;

            var content = await response.Content.ReadAsStringAsync();
            var data = System.Text.Json.JsonDocument.Parse(content);
            
            // Handle array response
            if (data.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var userJson in data.RootElement.EnumerateArray())
                {
                    var user = MapHttpResponseToUser(userJson);
                    if (user != null)
                        users.Add(user);
                }
            }
            // Handle object with users array
            else if (data.RootElement.TryGetProperty("users", out var usersArray))
            {
                foreach (var userJson in usersArray.EnumerateArray())
                {
                    var user = MapHttpResponseToUser(userJson);
                    if (user != null)
                        users.Add(user);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching users in HTTP resolver");
        }

        return users;
    }

    public override async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}{_checkPasswordPath}");
            AddHeaders(request);
            request.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { username = userId, password }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking password for user {UserId} in HTTP resolver", userId);
            return false;
        }
    }

    public override Task<IEnumerable<string>> GetUserGroupsAsync(string userId)
    {
        // HTTP resolver may not support groups
        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }

    public override async Task<Dictionary<string, string>> GetUserAttributesAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        return user?.Attributes ?? new Dictionary<string, string>();
    }

    private void AddHeaders(HttpRequestMessage request)
    {
        foreach (var (key, value) in _headers)
        {
            request.Headers.TryAddWithoutValidation(key, value);
        }
    }

    private ResolvedUser? MapHttpResponseToUser(System.Text.Json.JsonElement json)
    {
        try
        {
            var idField = GetConfigValue("USER_ID_FIELD", "id");
            var usernameField = GetConfigValue("USERNAME_FIELD", "username");
            var emailField = GetConfigValue("EMAIL_FIELD", "email");

            var user = new ResolvedUser
            {
                UserId = json.TryGetProperty(idField, out var id) ? id.ToString() : "",
                UserName = json.TryGetProperty(usernameField, out var un) ? un.GetString() ?? "" : "",
                Email = json.TryGetProperty(emailField, out var email) ? email.GetString() : null,
                ResolverName = ResolverEntity?.Name ?? "",
                Attributes = new Dictionary<string, string>()
            };

            // Add all string properties as attributes
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

/// <summary>
/// Passwd File Resolver (Unix /etc/passwd style)
/// Maps to Python: privacyidea/lib/resolvers/PasswdIdResolver.py
/// </summary>
public class PasswdResolver : ResolverBase
{
    public override string Type => "passwd";
    public override string DisplayName => "Passwd File Resolver";

    private string _fileName = "/etc/passwd";
    private List<PasswdEntry> _entries = new();

    public PasswdResolver(ILogger<PasswdResolver> logger) : base(logger)
    {
    }

    public override void Initialize(Resolver resolver, Dictionary<string, string> config)
    {
        base.Initialize(resolver, config);
        _fileName = GetConfigValue("fileName", "/etc/passwd");
        LoadPasswdFile();
    }

    private void LoadPasswdFile()
    {
        _entries.Clear();

        try
        {
            if (!File.Exists(_fileName))
            {
                Logger.LogWarning("Passwd file not found: {FileName}", _fileName);
                return;
            }

            foreach (var line in File.ReadLines(_fileName))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split(':');
                if (parts.Length >= 7)
                {
                    _entries.Add(new PasswdEntry
                    {
                        Username = parts[0],
                        Password = parts[1],
                        Uid = int.TryParse(parts[2], out var uid) ? uid : 0,
                        Gid = int.TryParse(parts[3], out var gid) ? gid : 0,
                        Gecos = parts[4],
                        HomeDir = parts[5],
                        Shell = parts[6]
                    });
                }
            }

            Logger.LogInformation("Loaded {Count} entries from passwd file", _entries.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading passwd file: {FileName}", _fileName);
        }
    }

    public override Task<ResolvedUser?> GetUserAsync(string userId)
    {
        var entry = _entries.FirstOrDefault(e => 
            e.Username.Equals(userId, StringComparison.OrdinalIgnoreCase) ||
            e.Uid.ToString() == userId);

        if (entry == null)
            return Task.FromResult<ResolvedUser?>(null);

        return Task.FromResult<ResolvedUser?>(MapEntryToUser(entry));
    }

    public override Task<IEnumerable<ResolvedUser>> SearchUsersAsync(string searchPattern, int maxResults = 100)
    {
        var users = _entries
            .Where(e => e.Username.Contains(searchPattern, StringComparison.OrdinalIgnoreCase) ||
                        e.Gecos.Contains(searchPattern, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .Select(MapEntryToUser)
            .ToList();

        return Task.FromResult<IEnumerable<ResolvedUser>>(users);
    }

    public override Task<bool> CheckPasswordAsync(string userId, string password)
    {
        // Passwd file typically contains hashed passwords or 'x' for shadow passwords
        // Direct password verification would require reading /etc/shadow
        Logger.LogWarning("Password verification not supported by passwd resolver (use PAM instead)");
        return Task.FromResult(false);
    }

    public override Task<IEnumerable<string>> GetUserGroupsAsync(string userId)
    {
        // Would need to read /etc/group file
        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }

    public override Task<Dictionary<string, string>> GetUserAttributesAsync(string userId)
    {
        var user = _entries.FirstOrDefault(e => 
            e.Username.Equals(userId, StringComparison.OrdinalIgnoreCase));
        
        if (user == null)
            return Task.FromResult(new Dictionary<string, string>());

        return Task.FromResult(new Dictionary<string, string>
        {
            ["username"] = user.Username,
            ["uid"] = user.Uid.ToString(),
            ["gid"] = user.Gid.ToString(),
            ["gecos"] = user.Gecos,
            ["homedir"] = user.HomeDir,
            ["shell"] = user.Shell
        });
    }

    public override Task<int> GetUserCountAsync()
    {
        return Task.FromResult(_entries.Count);
    }

    private ResolvedUser MapEntryToUser(PasswdEntry entry)
    {
        // Parse GECOS field (typically: Full Name,Room,Work Phone,Home Phone,Other)
        var gecosParts = entry.Gecos.Split(',');
        var fullName = gecosParts.Length > 0 ? gecosParts[0] : entry.Username;
        var nameParts = fullName.Split(' ');

        return new ResolvedUser
        {
            UserId = entry.Uid.ToString(),
            UserName = entry.Username,
            GivenName = nameParts.Length > 0 ? nameParts[0] : null,
            Surname = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : null,
            ResolverName = ResolverEntity?.Name ?? "",
            Attributes = new Dictionary<string, string>
            {
                ["uid"] = entry.Uid.ToString(),
                ["gid"] = entry.Gid.ToString(),
                ["gecos"] = entry.Gecos,
                ["homedir"] = entry.HomeDir,
                ["shell"] = entry.Shell
            }
        };
    }

    private class PasswdEntry
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int Uid { get; set; }
        public int Gid { get; set; }
        public string Gecos { get; set; } = "";
        public string HomeDir { get; set; } = "";
        public string Shell { get; set; } = "";
    }
}

/// <summary>
/// Static File Resolver (JSON/YAML user list)
/// Maps to Python: privacyidea/lib/resolvers/FileResolver.py
/// </summary>
public class FileResolver : ResolverBase
{
    public override string Type => "file";
    public override string DisplayName => "File Resolver";

    private string _fileName = "";
    private List<ResolvedUser> _users = new();

    public FileResolver(ILogger<FileResolver> logger) : base(logger)
    {
    }

    public override void Initialize(Resolver resolver, Dictionary<string, string> config)
    {
        base.Initialize(resolver, config);
        _fileName = GetConfigValue("fileName", "users.json");
        LoadUsersFile();
    }

    private void LoadUsersFile()
    {
        _users.Clear();

        try
        {
            if (!File.Exists(_fileName))
            {
                Logger.LogWarning("Users file not found: {FileName}", _fileName);
                return;
            }

            var content = File.ReadAllText(_fileName);
            
            if (_fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var usersData = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(content);
                if (usersData != null)
                {
                    foreach (var userData in usersData)
                    {
                        _users.Add(new ResolvedUser
                        {
                            UserId = userData.GetValueOrDefault("id", ""),
                            UserName = userData.GetValueOrDefault("username", ""),
                            Email = userData.GetValueOrDefault("email"),
                            GivenName = userData.GetValueOrDefault("givenName"),
                            Surname = userData.GetValueOrDefault("surname"),
                            Phone = userData.GetValueOrDefault("phone"),
                            Mobile = userData.GetValueOrDefault("mobile"),
                            ResolverName = ResolverEntity?.Name ?? "",
                            Attributes = userData
                        });
                    }
                }
            }

            Logger.LogInformation("Loaded {Count} users from file", _users.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading users file: {FileName}", _fileName);
        }
    }

    public override Task<ResolvedUser?> GetUserAsync(string userId)
    {
        var user = _users.FirstOrDefault(u => 
            u.UserId == userId || 
            u.UserName.Equals(userId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public override Task<IEnumerable<ResolvedUser>> SearchUsersAsync(string searchPattern, int maxResults = 100)
    {
        var users = _users
            .Where(u => u.UserName.Contains(searchPattern, StringComparison.OrdinalIgnoreCase) ||
                        (u.Email?.Contains(searchPattern, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (u.GivenName?.Contains(searchPattern, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (u.Surname?.Contains(searchPattern, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(maxResults)
            .ToList();

        return Task.FromResult<IEnumerable<ResolvedUser>>(users);
    }

    public override Task<bool> CheckPasswordAsync(string userId, string password)
    {
        // File resolver doesn't support password verification
        return Task.FromResult(false);
    }

    public override Task<IEnumerable<string>> GetUserGroupsAsync(string userId)
    {
        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }

    public override Task<Dictionary<string, string>> GetUserAttributesAsync(string userId)
    {
        var user = _users.FirstOrDefault(u => u.UserId == userId || u.UserName == userId);
        return Task.FromResult(user?.Attributes ?? new Dictionary<string, string>());
    }

    public override Task<int> GetUserCountAsync()
    {
        return Task.FromResult(_users.Count);
    }
}

/// <summary>
/// Keycloak User Resolver
/// Maps to Python: privacyidea/lib/resolvers/KeycloakResolver.py
/// Red Hat SSO / Keycloak identity management
/// </summary>
public class KeycloakResolver : ResolverBase
{
    public override string Type => "keycloak";
    public override string DisplayName => "Keycloak Resolver";

    private string _baseUrl = "";
    private string _realm = "";
    private string _clientId = "admin-cli";
    private string _adminUsername = "";
    private string _adminPassword = "";
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly HttpClient _httpClient;

    // Attribute mappings
    private readonly Dictionary<string, string> _piToKeycloak = new()
    {
        { "username", "username" },
        { "userid", "id" },
        { "givenname", "firstName" },
        { "surname", "lastName" },
        { "email", "email" }
    };

    private readonly Dictionary<string, string> _keycloakToPi;

    public KeycloakResolver(ILogger<KeycloakResolver> logger, HttpClient? httpClient = null) : base(logger)
    {
        _httpClient = httpClient ?? new HttpClient();
        _keycloakToPi = _piToKeycloak.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }

    public override void Initialize(Resolver resolver, Dictionary<string, string> config)
    {
        base.Initialize(resolver, config);

        _baseUrl = GetConfigValue("Endpoint", "http://localhost:8080").TrimEnd('/');
        _realm = GetConfigValue("realm", "master");
        _clientId = GetConfigValue("clientId", "admin-cli");
        _adminUsername = GetConfigValue("adminUsername", "");
        _adminPassword = GetConfigValue("adminPassword", "");
    }

    private async Task<bool> EnsureAuthenticatedAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            return true;

        try
        {
            var tokenUrl = $"{_baseUrl}/realms/{_realm}/protocol/openid-connect/token";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", _clientId },
                { "username", _adminUsername },
                { "password", _adminPassword }
            });

            var response = await _httpClient.PostAsync(tokenUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("Keycloak authentication failed: {Status}", response.StatusCode);
                return false;
            }

            var tokenData = await response.Content.ReadAsStringAsync();
            var json = System.Text.Json.JsonDocument.Parse(tokenData);
            
            _accessToken = json.RootElement.GetProperty("access_token").GetString();
            var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 30); // 30 second buffer

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating with Keycloak");
            return false;
        }
    }

    public override async Task<ResolvedUser?> GetUserAsync(string userId)
    {
        if (!await EnsureAuthenticatedAsync())
            return null;

        try
        {
            var url = $"{_baseUrl}/admin/realms/{_realm}/users/{Uri.EscapeDataString(userId)}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var userData = System.Text.Json.JsonDocument.Parse(content);
            return MapKeycloakUserToResolvedUser(userData.RootElement);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting user {UserId} from Keycloak", userId);
            return null;
        }
    }

    public async Task<ResolvedUser?> GetUserByNameAsync(string userName)
    {
        if (!await EnsureAuthenticatedAsync())
            return null;

        try
        {
            var url = $"{_baseUrl}/admin/realms/{_realm}/users?username={Uri.EscapeDataString(userName)}&exact=true";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var users = System.Text.Json.JsonDocument.Parse(content);
            
            foreach (var userJson in users.RootElement.EnumerateArray())
            {
                return MapKeycloakUserToResolvedUser(userJson);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting user by name {UserName} from Keycloak", userName);
            return null;
        }
    }

    public override async Task<IEnumerable<ResolvedUser>> SearchUsersAsync(string searchPattern, int maxResults = 100)
    {
        var users = new List<ResolvedUser>();

        if (!await EnsureAuthenticatedAsync())
            return users;

        try
        {
            var url = $"{_baseUrl}/admin/realms/{_realm}/users?search={Uri.EscapeDataString(searchPattern)}&max={maxResults}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return users;

            var content = await response.Content.ReadAsStringAsync();
            var usersData = System.Text.Json.JsonDocument.Parse(content);
            
            foreach (var userJson in usersData.RootElement.EnumerateArray())
            {
                var user = MapKeycloakUserToResolvedUser(userJson);
                if (user != null)
                    users.Add(user);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching users in Keycloak");
        }

        return users;
    }

    public override async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        try
        {
            var user = await GetUserAsync(userId) ?? await GetUserByNameAsync(userId);
            if (user == null)
                return false;

            var tokenUrl = $"{_baseUrl}/realms/{_realm}/protocol/openid-connect/token";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", _clientId },
                { "username", user.UserName },
                { "password", password }
            });

            var response = await _httpClient.PostAsync(tokenUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating user {UserId} with Keycloak", userId);
            return false;
        }
    }

    public override async Task<IEnumerable<string>> GetUserGroupsAsync(string userId)
    {
        var groups = new List<string>();

        if (!await EnsureAuthenticatedAsync())
            return groups;

        try
        {
            var url = $"{_baseUrl}/admin/realms/{_realm}/users/{Uri.EscapeDataString(userId)}/groups";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return groups;

            var content = await response.Content.ReadAsStringAsync();
            var groupsData = System.Text.Json.JsonDocument.Parse(content);
            
            foreach (var groupJson in groupsData.RootElement.EnumerateArray())
            {
                if (groupJson.TryGetProperty("name", out var name))
                    groups.Add(name.GetString() ?? "");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting groups for user {UserId} from Keycloak", userId);
        }

        return groups;
    }

    public override async Task<Dictionary<string, string>> GetUserAttributesAsync(string userId)
    {
        var attributes = new Dictionary<string, string>();

        if (!await EnsureAuthenticatedAsync())
            return attributes;

        try
        {
            var url = $"{_baseUrl}/admin/realms/{_realm}/users/{Uri.EscapeDataString(userId)}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return attributes;

            var content = await response.Content.ReadAsStringAsync();
            var userData = System.Text.Json.JsonDocument.Parse(content);
            
            if (userData.RootElement.TryGetProperty("attributes", out var attrs))
            {
                foreach (var attr in attrs.EnumerateObject())
                {
                    if (attr.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var values = attr.Value.EnumerateArray()
                            .Select(v => v.GetString() ?? "")
                            .ToArray();
                        attributes[attr.Name] = string.Join(",", values);
                    }
                    else
                    {
                        attributes[attr.Name] = attr.Value.GetString() ?? "";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting attributes for user {UserId} from Keycloak", userId);
        }

        return attributes;
    }

    public override async Task<int> GetUserCountAsync()
    {
        if (!await EnsureAuthenticatedAsync())
            return 0;

        try
        {
            var url = $"{_baseUrl}/admin/realms/{_realm}/users/count";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return 0;

            var content = await response.Content.ReadAsStringAsync();
            return int.TryParse(content, out var count) ? count : 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting user count from Keycloak");
            return 0;
        }
    }

    private ResolvedUser? MapKeycloakUserToResolvedUser(System.Text.Json.JsonElement userJson)
    {
        try
        {
            var user = new ResolvedUser
            {
                ResolverName = ResolverEntity?.Name ?? "",
                Attributes = new Dictionary<string, string>()
            };

            if (userJson.TryGetProperty("id", out var id))
                user.UserId = id.GetString() ?? "";

            if (userJson.TryGetProperty("username", out var username))
                user.UserName = username.GetString() ?? "";

            if (userJson.TryGetProperty("email", out var email))
                user.Email = email.GetString();

            if (userJson.TryGetProperty("firstName", out var firstName))
                user.GivenName = firstName.GetString();

            if (userJson.TryGetProperty("lastName", out var lastName))
                user.Surname = lastName.GetString();

            // Map standard attributes
            foreach (var (keycloakKey, piKey) in _keycloakToPi)
            {
                if (userJson.TryGetProperty(keycloakKey, out var value) && 
                    value.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    user.Attributes[piKey] = value.GetString() ?? "";
                }
            }

            // Map custom attributes
            if (userJson.TryGetProperty("attributes", out var attrs))
            {
                foreach (var attr in attrs.EnumerateObject())
                {
                    if (attr.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var values = attr.Value.EnumerateArray()
                            .Select(v => v.GetString() ?? "")
                            .ToArray();
                        user.Attributes[attr.Name] = string.Join(",", values);
                    }
                }
            }

            return user;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error mapping Keycloak user");
            return null;
        }
    }
}
