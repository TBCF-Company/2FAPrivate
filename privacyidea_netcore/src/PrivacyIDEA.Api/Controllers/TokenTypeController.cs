using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Tokens;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Token type information API
/// </summary>
[ApiController]
[Route("ttype")]
public class TokenTypeController : ControllerBase
{
    private static readonly Dictionary<string, TokenTypeInfo> TokenTypes = new()
    {
        ["hotp"] = new TokenTypeInfo
        {
            Type = "hotp",
            Title = "HOTP",
            Description = "HMAC-based One-Time Password (RFC 4226)",
            Class = "HotpToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["otplen"] = "Length of the OTP value (default: 6)",
                ["hashlib"] = "Hash algorithm: sha1, sha256, sha512 (default: sha1)",
                ["counter"] = "Initial counter value (default: 0)"
            }
        },
        ["totp"] = new TokenTypeInfo
        {
            Type = "totp",
            Title = "TOTP",
            Description = "Time-based One-Time Password (RFC 6238)",
            Class = "TotpToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["otplen"] = "Length of the OTP value (default: 6)",
                ["hashlib"] = "Hash algorithm: sha1, sha256, sha512 (default: sha1)",
                ["timeStep"] = "Time step in seconds (default: 30)",
                ["timeShift"] = "Time offset in seconds (default: 0)"
            }
        },
        ["sms"] = new TokenTypeInfo
        {
            Type = "sms",
            Title = "SMS",
            Description = "Send OTP via SMS",
            Class = "SmsToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["phone"] = "Phone number to send SMS to",
                ["smsgateway"] = "SMS gateway configuration to use"
            }
        },
        ["email"] = new TokenTypeInfo
        {
            Type = "email",
            Title = "Email",
            Description = "Send OTP via Email",
            Class = "EmailToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["email"] = "Email address to send OTP to",
                ["smtp_server"] = "SMTP server configuration to use"
            }
        },
        ["push"] = new TokenTypeInfo
        {
            Type = "push",
            Title = "Push",
            Description = "Push notification authentication",
            Class = "PushToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["firebase_config"] = "Firebase configuration for push notifications"
            }
        },
        ["webauthn"] = new TokenTypeInfo
        {
            Type = "webauthn",
            Title = "WebAuthn",
            Description = "WebAuthn/FIDO2 authentication",
            Class = "WebAuthnToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["credential_id"] = "WebAuthn credential ID",
                ["public_key"] = "WebAuthn public key"
            }
        },
        ["certificate"] = new TokenTypeInfo
        {
            Type = "certificate",
            Title = "Certificate",
            Description = "X.509 certificate-based authentication",
            Class = "CertificateToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["certificate"] = "X.509 certificate in PEM format"
            }
        },
        ["sshkey"] = new TokenTypeInfo
        {
            Type = "sshkey",
            Title = "SSH Key",
            Description = "SSH public key authentication",
            Class = "SshKeyToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["sshkey"] = "SSH public key"
            }
        },
        ["password"] = new TokenTypeInfo
        {
            Type = "password",
            Title = "Password",
            Description = "Static password token",
            Class = "PasswordToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["password"] = "Static password value"
            }
        },
        ["registration"] = new TokenTypeInfo
        {
            Type = "registration",
            Title = "Registration",
            Description = "One-time registration code",
            Class = "RegistrationToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["validity"] = "Validity period in minutes"
            }
        },
        ["paper"] = new TokenTypeInfo
        {
            Type = "paper",
            Title = "Paper/TAN List",
            Description = "List of one-time passwords on paper",
            Class = "PaperToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["count"] = "Number of TANs to generate"
            }
        },
        ["radius"] = new TokenTypeInfo
        {
            Type = "radius",
            Title = "RADIUS",
            Description = "Forward authentication to RADIUS server",
            Class = "RadiusToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["radius.server"] = "RADIUS server address",
                ["radius.secret"] = "RADIUS shared secret"
            }
        },
        ["yubico"] = new TokenTypeInfo
        {
            Type = "yubico",
            Title = "Yubico",
            Description = "Yubico Cloud authentication",
            Class = "YubicoToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["yubico.id"] = "Yubico API ID",
                ["yubico.apikey"] = "Yubico API key"
            }
        },
        ["yubikey"] = new TokenTypeInfo
        {
            Type = "yubikey",
            Title = "YubiKey",
            Description = "Local YubiKey AES authentication",
            Class = "YubiKeyToken",
            ConfigDescription = new Dictionary<string, string>
            {
                ["aes_key"] = "YubiKey AES key"
            }
        }
    };

    /// <summary>
    /// Get all available token types
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new
        {
            result = new
            {
                value = TokenTypes.Values.Select(t => new
                {
                    t.Type,
                    t.Title,
                    t.Description
                })
            },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get a specific token type
    /// </summary>
    [HttpGet("{type}")]
    public IActionResult Get(string type)
    {
        if (!TokenTypes.TryGetValue(type.ToLower(), out var tokenType))
            return NotFound(new { result = new { status = false }, detail = $"Token type '{type}' not found" });

        return Ok(new
        {
            result = new { value = tokenType },
            version = "1.0",
            id = 1
        });
    }
}

public class TokenTypeInfo
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public Dictionary<string, string> ConfigDescription { get; set; } = new();
}
