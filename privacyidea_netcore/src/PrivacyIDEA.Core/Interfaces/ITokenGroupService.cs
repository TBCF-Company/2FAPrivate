namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Service for managing token groups
/// </summary>
public interface ITokenGroupService
{
    /// <summary>
    /// Get all token groups
    /// </summary>
    Task<IEnumerable<TokenGroupInfo>> GetGroupsAsync();

    /// <summary>
    /// Get a specific token group by name
    /// </summary>
    Task<TokenGroupInfo?> GetGroupAsync(string name);

    /// <summary>
    /// Create a token group
    /// </summary>
    Task<TokenGroupInfo> CreateGroupAsync(string name, string? description = null);

    /// <summary>
    /// Update a token group
    /// </summary>
    Task<TokenGroupInfo?> UpdateGroupAsync(string name, string? description);

    /// <summary>
    /// Delete a token group
    /// </summary>
    Task<bool> DeleteGroupAsync(string name);

    /// <summary>
    /// Add a token to a group
    /// </summary>
    Task<bool> AddTokenToGroupAsync(string groupName, string tokenSerial);

    /// <summary>
    /// Remove a token from a group
    /// </summary>
    Task<bool> RemoveTokenFromGroupAsync(string groupName, string tokenSerial);

    /// <summary>
    /// Get all tokens in a group
    /// </summary>
    Task<IEnumerable<string>> GetTokensInGroupAsync(string groupName);

    /// <summary>
    /// Get all groups a token belongs to
    /// </summary>
    Task<IEnumerable<string>> GetGroupsForTokenAsync(string tokenSerial);
}

public class TokenGroupInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TokenCount { get; set; }
    public IEnumerable<string> TokenSerials { get; set; } = Array.Empty<string>();
}
