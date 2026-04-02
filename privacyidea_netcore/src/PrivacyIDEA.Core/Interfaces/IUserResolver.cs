namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Interface for user resolver implementations
/// Maps to Python: privacyidea/lib/resolvers/UserIdResolver.py
/// </summary>
public interface IUserResolver
{
    /// <summary>
    /// Get the resolver type name
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Get the display name
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Initialize the resolver with configuration
    /// </summary>
    void Initialize(Domain.Entities.Resolver resolver, Dictionary<string, string> config);

    /// <summary>
    /// Get user by user ID
    /// </summary>
    Task<ResolvedUser?> GetUserAsync(string userId);

    /// <summary>
    /// Search users with pattern
    /// </summary>
    Task<IEnumerable<ResolvedUser>> SearchUsersAsync(string searchPattern, int maxResults = 100);

    /// <summary>
    /// Authenticate user with password
    /// </summary>
    Task<bool> CheckPasswordAsync(string userId, string password);

    /// <summary>
    /// Get user's groups
    /// </summary>
    Task<IEnumerable<string>> GetUserGroupsAsync(string userId);

    /// <summary>
    /// Get user's attributes
    /// </summary>
    Task<Dictionary<string, string>> GetUserAttributesAsync(string userId);

    /// <summary>
    /// Test the resolver connection
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Get total user count
    /// </summary>
    Task<int> GetUserCountAsync();
}

/// <summary>
/// User data from resolver
/// </summary>
public class ResolvedUser
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Dn { get; set; }
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
    public string ResolverName { get; set; } = string.Empty;
}

/// <summary>
/// User search filter (for complex searches)
/// </summary>
public class UserSearchFilter
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, string>? CustomAttributes { get; set; }
}

/// <summary>
/// Paginated result
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Result of resolver configuration test
/// </summary>
public class ResolverTestResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? UserCount { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Request to create a new user
/// </summary>
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
}
