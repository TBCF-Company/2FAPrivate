using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Interface for Authentication Service
/// Maps to Python: privacyidea/lib/auth.py
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate an admin user
    /// </summary>
    Task<AuthResult> AuthenticateAdminAsync(string username, string password);

    /// <summary>
    /// Authenticate a user with optional second factor
    /// </summary>
    Task<AuthResult> AuthenticateUserAsync(string username, string? realm, string? password, string? otp);

    /// <summary>
    /// Validate an authentication token (JWT)
    /// </summary>
    Task<TokenValidationResult> ValidateAuthTokenAsync(string token);

    /// <summary>
    /// Refresh an authentication token
    /// </summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revoke an authentication token
    /// </summary>
    Task<bool> RevokeTokenAsync(string token);

    /// <summary>
    /// Get user rights/permissions
    /// </summary>
    Task<UserRights> GetUserRightsAsync(string username, string? realm = null);

    /// <summary>
    /// Check if user has specific permission
    /// </summary>
    Task<bool> HasPermissionAsync(string username, string permission, string? realm = null);

    /// <summary>
    /// Get admin by username
    /// </summary>
    Task<Admin?> GetAdminAsync(string username);

    /// <summary>
    /// Create new admin
    /// </summary>
    Task<Admin> CreateAdminAsync(string username, string password, string? email = null);

    /// <summary>
    /// Update admin password
    /// </summary>
    Task<bool> UpdateAdminPasswordAsync(string username, string newPassword);

    /// <summary>
    /// Delete admin
    /// </summary>
    Task<bool> DeleteAdminAsync(string username);

    /// <summary>
    /// List all admins
    /// </summary>
    Task<IEnumerable<Admin>> ListAdminsAsync();

    /// <summary>
    /// Check password against stored hash
    /// </summary>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Hash a password
    /// </summary>
    string HashPassword(string password);
}

/// <summary>
/// Authentication result
/// </summary>
public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Username { get; set; }
    public string? Realm { get; set; }
    public string? Role { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiresSecondFactor { get; set; }
    public string? TransactionId { get; set; }
    public List<string> Rights { get; set; } = new();
}

/// <summary>
/// Token validation result
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? Username { get; set; }
    public string? Realm { get; set; }
    public string? Role { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string> Rights { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// User rights/permissions
/// </summary>
public class UserRights
{
    public string Username { get; set; } = string.Empty;
    public string? Realm { get; set; }
    public string Role { get; set; } = "user";
    public List<string> Permissions { get; set; } = new();
    public List<string> Realms { get; set; } = new();
    public bool IsAdmin { get; set; }
    public Dictionary<string, object> PolicyRights { get; set; } = new();
}
