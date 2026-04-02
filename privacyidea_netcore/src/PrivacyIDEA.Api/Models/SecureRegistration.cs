using System.ComponentModel.DataAnnotations;

namespace PrivacyIDEA.Api.Models;

/// <summary>
/// Request for secure token registration with RSA key exchange
/// Client provides their RSA public key, server encrypts seed with it
/// </summary>
public class SecureRegisterRequest
{
    /// <summary>
    /// RSA public key in Base64 format (SPKI/X.509 or PKCS#1)
    /// Can also be PEM format with BEGIN/END markers
    /// </summary>
    [Required]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Token type (totp, hotp)
    /// </summary>
    public string Type { get; set; } = "totp";

    /// <summary>
    /// Username to assign token to
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Realm for the user
    /// </summary>
    public string? Realm { get; set; }

    /// <summary>
    /// Unique device identifier for device binding
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Device information for tracking/audit
    /// </summary>
    public DeviceInfo? DeviceInfo { get; set; }

    /// <summary>
    /// Registration code (if required by policy)
    /// </summary>
    public string? RegistrationCode { get; set; }

    /// <summary>
    /// Token description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Hash algorithm (sha1, sha256, sha512)
    /// </summary>
    public string Algorithm { get; set; } = "sha1";

    /// <summary>
    /// Number of OTP digits (6 or 8)
    /// </summary>
    [Range(6, 8)]
    public int Digits { get; set; } = 6;

    /// <summary>
    /// TOTP period in seconds
    /// </summary>
    [Range(30, 60)]
    public int Period { get; set; } = 30;
}

/// <summary>
/// Device information for binding and audit
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// Device model (e.g., "iPhone 15 Pro", "Samsung Galaxy S24")
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Operating system (e.g., "iOS", "Android")
    /// </summary>
    public string? OS { get; set; }

    /// <summary>
    /// OS version (e.g., "17.4", "14")
    /// </summary>
    public string? OSVersion { get; set; }

    /// <summary>
    /// Application version
    /// </summary>
    public string? AppVersion { get; set; }

    /// <summary>
    /// Device fingerprint for additional security
    /// </summary>
    public string? Fingerprint { get; set; }
}

/// <summary>
/// Response for secure token registration
/// Contains encrypted seed that only client can decrypt
/// </summary>
public class SecureRegisterResponse
{
    /// <summary>
    /// Operation status
    /// </summary>
    public bool Status { get; set; }

    /// <summary>
    /// Token serial number
    /// </summary>
    public string Serial { get; set; } = string.Empty;

    /// <summary>
    /// Token type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Seed encrypted with client's RSA public key (Base64)
    /// Client must decrypt with their private key
    /// </summary>
    public string EncryptedSeed { get; set; } = string.Empty;

    /// <summary>
    /// Hash algorithm used
    /// </summary>
    public string Algorithm { get; set; } = "sha1";

    /// <summary>
    /// Number of OTP digits
    /// </summary>
    public int Digits { get; set; } = 6;

    /// <summary>
    /// TOTP period in seconds
    /// </summary>
    public int Period { get; set; } = 30;

    /// <summary>
    /// Issuer name for OTP app display
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// Device binding ID (if device binding was requested)
    /// </summary>
    public string? DeviceBindingId { get; set; }

    /// <summary>
    /// Error message (if status is false)
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Configuration for secure registration
/// </summary>
public class SecureRegistrationConfig
{
    /// <summary>
    /// Enable secure registration endpoint
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Require device binding for all registrations
    /// </summary>
    public bool RequireDeviceBinding { get; set; } = false;

    /// <summary>
    /// Minimum RSA key size in bits
    /// </summary>
    public int MinKeySize { get; set; } = 2048;

    /// <summary>
    /// Maximum requests per minute per IP
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 10;

    /// <summary>
    /// Allowed token types for secure registration
    /// </summary>
    public string[] AllowedTypes { get; set; } = new[] { "totp", "hotp" };
}
