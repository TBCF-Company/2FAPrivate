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
