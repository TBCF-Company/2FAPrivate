namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Interface for User Service
/// Maps to Python: privacyidea/lib/user.py
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get user by login name
    /// </summary>
    Task<UserInfo?> GetUserAsync(string username, string? realm = null);

    /// <summary>
    /// Search users across resolvers
    /// </summary>
    Task<PagedResult<UserInfo>> SearchUsersAsync(UserSearchFilter filter, int page = 1, int pageSize = 15);

    /// <summary>
    /// Get user from specific resolver
    /// </summary>
    Task<UserInfo?> GetUserFromResolverAsync(string username, string resolverName);

    /// <summary>
    /// Get users in a realm
    /// </summary>
    Task<IEnumerable<UserInfo>> GetUsersInRealmAsync(string realm);

    /// <summary>
    /// Create user (for editable resolvers)
    /// </summary>
    Task<UserInfo> CreateUserAsync(UserCreateRequest request);

    /// <summary>
    /// Update user (for editable resolvers)
    /// </summary>
    Task<bool> UpdateUserAsync(string username, string? realm, UserUpdateRequest request);

    /// <summary>
    /// Delete user (for editable resolvers)
    /// </summary>
    Task<bool> DeleteUserAsync(string username, string? realm);

    /// <summary>
    /// Get user attributes
    /// </summary>
    Task<Dictionary<string, string>> GetUserAttributesAsync(string username, string? realm = null);

    /// <summary>
    /// Set user attribute
    /// </summary>
    Task<bool> SetUserAttributeAsync(string username, string? realm, string key, string value);

    /// <summary>
    /// Delete user attribute
    /// </summary>
    Task<bool> DeleteUserAttributeAsync(string username, string? realm, string key);

    /// <summary>
    /// Check if user exists
    /// </summary>
    Task<bool> UserExistsAsync(string username, string? realm = null);

    /// <summary>
    /// Get resolvers for a realm
    /// </summary>
    Task<IEnumerable<string>> GetResolversForRealmAsync(string realm);

    /// <summary>
    /// Get all available realms
    /// </summary>
    Task<IEnumerable<string>> GetRealmsAsync();

    /// <summary>
    /// Get the default realm
    /// </summary>
    Task<string?> GetDefaultRealmAsync();
}

/// <summary>
/// User information
/// </summary>
public class UserInfo
{
    public string Username { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Realm { get; set; }
    public string? Resolver { get; set; }
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Description { get; set; }
    public bool Editable { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
}

/// <summary>
/// User create request
/// </summary>
public class UserCreateRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Realm { get; set; }
    public string? Resolver { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
}

/// <summary>
/// User update request
/// </summary>
public class UserUpdateRequest
{
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Description { get; set; }
    public string? Password { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
}
