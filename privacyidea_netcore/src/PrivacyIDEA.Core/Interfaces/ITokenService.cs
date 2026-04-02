using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Interface for Token Service - manages token lifecycle
/// Maps to Python: privacyidea/lib/token.py
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Get token by serial
    /// </summary>
    Task<Token?> GetTokenBySerialAsync(string serial);

    /// <summary>
    /// Get tokens for a user
    /// </summary>
    Task<IEnumerable<Token>> GetTokensForUserAsync(string userId, string? realm = null);

    /// <summary>
    /// Search tokens with filters
    /// </summary>
    Task<PagedResult<Token>> SearchTokensAsync(TokenSearchFilter filter, int page = 1, int pageSize = 15);

    /// <summary>
    /// Create/Initialize a new token
    /// </summary>
    Task<Token> InitTokenAsync(TokenInitParameters parameters);

    /// <summary>
    /// Delete a token
    /// </summary>
    Task<bool> DeleteTokenAsync(string serial);

    /// <summary>
    /// Enable a token
    /// </summary>
    Task<bool> EnableTokenAsync(string serial);

    /// <summary>
    /// Disable a token
    /// </summary>
    Task<bool> DisableTokenAsync(string serial);

    /// <summary>
    /// Assign token to user
    /// </summary>
    Task<bool> AssignTokenAsync(string serial, string userId, string? resolver = null, string? realm = null);

    /// <summary>
    /// Unassign token from user
    /// </summary>
    Task<bool> UnassignTokenAsync(string serial);

    /// <summary>
    /// Set PIN for token
    /// </summary>
    Task<bool> SetPinAsync(string serial, string pin);

    /// <summary>
    /// Reset fail counter
    /// </summary>
    Task<bool> ResetFailCounterAsync(string serial);

    /// <summary>
    /// Resync a token
    /// </summary>
    Task<bool> ResyncTokenAsync(string serial, string otp1, string otp2);

    /// <summary>
    /// Get token class implementation
    /// </summary>
    ITokenClass GetTokenClassForType(string tokenType);

    /// <summary>
    /// Set token info
    /// </summary>
    Task<bool> SetTokenInfoAsync(string serial, string key, string value);

    /// <summary>
    /// Get token info
    /// </summary>
    Task<Dictionary<string, string>> GetTokenInfoAsync(string serial);

    /// <summary>
    /// Revoke a token
    /// </summary>
    Task<bool> RevokeTokenAsync(string serial);

    /// <summary>
    /// Get token count statistics
    /// </summary>
    Task<TokenStatistics> GetStatisticsAsync();
}

/// <summary>
/// Token search filter
/// </summary>
public class TokenSearchFilter
{
    public string? Serial { get; set; }
    public string? TokenType { get; set; }
    public string? UserId { get; set; }
    public string? Realm { get; set; }
    public bool? Active { get; set; }
    public bool? Assigned { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Parameters for initializing a new token
/// </summary>
public class TokenInitParameters
{
    public string Type { get; set; } = "hotp";
    public string? Serial { get; set; }
    public string? UserId { get; set; }
    public string? Realm { get; set; }
    public string? Resolver { get; set; }
    public string? Description { get; set; }
    public string? Pin { get; set; }
    public byte[]? OtpKey { get; set; }
    public int OtpLen { get; set; } = 6;
    public bool GenerateKey { get; set; } = true;
    public string HashAlgorithm { get; set; } = "sha1";
    public int TimeStep { get; set; } = 30;
    public Dictionary<string, string>? Info { get; set; }
}

/// <summary>
/// Token statistics
/// </summary>
public class TokenStatistics
{
    public int TotalTokens { get; set; }
    public int ActiveTokens { get; set; }
    public int AssignedTokens { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
    public Dictionary<string, int> ByRealm { get; set; } = new();
}
