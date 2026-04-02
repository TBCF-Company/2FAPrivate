namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Interface for token class implementations
/// Maps to Python: privacyidea/lib/tokenclass.py - TokenClass
/// </summary>
public interface ITokenClass
{
    /// <summary>
    /// Get the token type name
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Get display name for the token type
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Check if this token supports challenge-response
    /// </summary>
    bool SupportsChallengeResponse { get; }

    /// <summary>
    /// Check if this token supports offline authentication
    /// </summary>
    bool SupportsOffline { get; }

    /// <summary>
    /// Initialize the token with database entity
    /// </summary>
    void Initialize(Domain.Entities.Token tokenEntity);

    /// <summary>
    /// Authenticate with PIN and OTP
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp);

    /// <summary>
    /// Check only the OTP value
    /// </summary>
    Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null);

    /// <summary>
    /// Check only the PIN
    /// </summary>
    Task<bool> CheckPinAsync(string pin);

    /// <summary>
    /// Create a challenge for challenge-response authentication
    /// </summary>
    Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null);

    /// <summary>
    /// Update token properties after successful authentication
    /// </summary>
    Task UpdateAsync();

    /// <summary>
    /// Get token information as dictionary
    /// </summary>
    Dictionary<string, object> GetTokenInfo();

    /// <summary>
    /// Set token information
    /// </summary>
    void SetTokenInfo(string key, object value);

    /// <summary>
    /// Get the OTP value (for testing/enrollment)
    /// </summary>
    Task<string?> GetOtpAsync(long? timestamp = null);

    /// <summary>
    /// Resynchronize the token
    /// </summary>
    Task<bool> ResyncAsync(string otp1, string otp2);

    /// <summary>
    /// Set a new PIN for the token
    /// </summary>
    Task SetPinAsync(string pin);

    /// <summary>
    /// Reset the fail counter
    /// </summary>
    Task ResetFailCounterAsync();

    /// <summary>
    /// Increment the fail counter
    /// </summary>
    Task IncrementFailCounterAsync();

    /// <summary>
    /// Check if token is active and usable
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Check if token is locked due to too many failed attempts
    /// </summary>
    bool IsLocked { get; }
}

/// <summary>
/// Result of authentication attempt
/// </summary>
public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object>? Details { get; set; }
    public bool PinCorrect { get; set; }
    public bool OtpCorrect { get; set; }
}

/// <summary>
/// Result of challenge creation
/// </summary>
public class ChallengeResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? Message { get; set; }
    public string? Challenge { get; set; }
    public Dictionary<string, object>? Attributes { get; set; }
}
