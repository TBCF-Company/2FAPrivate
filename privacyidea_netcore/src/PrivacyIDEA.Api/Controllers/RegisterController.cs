using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Api.Models;
using PrivacyIDEA.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Token Registration API
/// Maps to Python: privacyidea/api/register.py
/// </summary>
[ApiController]
[Route("register")]
public class RegisterController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IPolicyService _policyService;
    private readonly IAuditService _auditService;
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<RegisterController> _logger;

    public RegisterController(
        ITokenService tokenService,
        IPolicyService policyService,
        IAuditService auditService,
        ICryptoService cryptoService,
        ILogger<RegisterController> logger)
    {
        _tokenService = tokenService;
        _policyService = policyService;
        _auditService = auditService;
        _cryptoService = cryptoService;
        _logger = logger;
    }

    /// <summary>
    /// POST /register - Register a new token with registration code
    /// Self-registration endpoint for users
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Check if self-registration is enabled via policy
        var policies = await _policyService.GetPoliciesAsync("enrollment");
        var allowSelfReg = policies.Any(p => p.Action == "selfregistration" && p.Active);

        if (!allowSelfReg && string.IsNullOrEmpty(request.RegistrationCode))
        {
            return BadRequest(new
            {
                jsonrpc = "2.0",
                result = new
                {
                    status = false,
                    error = new { message = "Self-registration is not enabled" }
                }
            });
        }

        // Validate registration code if provided
        if (!string.IsNullOrEmpty(request.RegistrationCode))
        {
            var isValid = await ValidateRegistrationCodeAsync(request.RegistrationCode);
            if (!isValid)
            {
                await _auditService.LogAsync("register", false, request.User ?? "unknown",
                    request.Realm, null, request.Type, "Invalid registration code");

                return BadRequest(new
                {
                    jsonrpc = "2.0",
                    result = new
                    {
                        status = false,
                        error = new { message = "Invalid registration code" }
                    }
                });
            }
        }

        // Generate token based on type
        var tokenType = request.Type?.ToUpperInvariant() ?? "TOTP";
        var serial = await GenerateSerialAsync(tokenType);

        // Create the token
        var result = await _tokenService.InitTokenAsync(new Core.Interfaces.TokenInitRequest
        {
            Type = tokenType,
            Serial = serial,
            User = request.User,
            Realm = request.Realm,
            GenerateKey = true,
            Description = request.Description ?? "Self-registered token"
        });

        if (!result.Success)
        {
            await _auditService.LogAsync("register", false, request.User ?? "unknown",
                request.Realm, serial, tokenType, result.Message);

            return BadRequest(new
            {
                jsonrpc = "2.0",
                result = new
                {
                    status = false,
                    error = new { message = result.Message }
                }
            });
        }

        await _auditService.LogAsync("register", true, request.User ?? "unknown",
            request.Realm, serial, tokenType, "Token registered successfully");

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = new
                {
                    serial = result.Serial,
                    type = tokenType,
                    googleurl = result.GoogleUrl,
                    otpauth = result.OtpAuthUrl,
                    qrcode = result.QrCode
                }
            },
            detail = new
            {
                serial = result.Serial,
                googleurl = new { description = "Google Authenticator URL", img = result.QrCode }
            }
        });
    }

    /// <summary>
    /// GET /register/status - Check registration status
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRegistrationStatus()
    {
        var policies = await _policyService.GetPoliciesAsync("enrollment");
        var selfRegEnabled = policies.Any(p => p.Action == "selfregistration" && p.Active);
        var codeRequired = policies.Any(p => p.Action == "registration_code_required" && p.Active);

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = new
                {
                    enabled = selfRegEnabled,
                    code_required = codeRequired,
                    allowed_types = new[] { "TOTP", "HOTP", "PUSH" }
                }
            }
        });
    }

    /// <summary>
    /// POST /register/secure - Secure token registration with RSA key exchange
    /// Client provides RSA public key, server encrypts seed with it
    /// Designed for banking apps and high-security scenarios
    /// </summary>
    [HttpPost("secure")]
    [AllowAnonymous]
    public async Task<IActionResult> SecureRegister([FromBody] SecureRegisterRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // 1. Validate RSA public key
        byte[] publicKeyBytes;
        try
        {
            publicKeyBytes = _cryptoService.ImportRsaPublicKeyFromBase64(request.PublicKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Invalid RSA public key from {IP}: {Error}", clientIp, ex.Message);
            
            await _auditService.LogAsync("secure_register", false, request.User ?? "unknown",
                request.Realm, null, request.Type, "Invalid RSA public key format");

            return BadRequest(new SecureRegisterResponse
            {
                Status = false,
                Error = "Invalid RSA public key format. Please provide a valid Base64-encoded RSA public key."
            });
        }

        // 2. Validate key size (minimum 2048 bits)
        var (isValid, keyError, keySize) = _cryptoService.ValidateRsaPublicKey(publicKeyBytes, 2048);
        if (!isValid)
        {
            _logger.LogWarning("RSA key validation failed from {IP}: {Error}", clientIp, keyError);
            
            await _auditService.LogAsync("secure_register", false, request.User ?? "unknown",
                request.Realm, null, request.Type, $"RSA key validation failed: {keyError}");

            return BadRequest(new SecureRegisterResponse
            {
                Status = false,
                Error = keyError
            });
        }

        _logger.LogInformation("Secure registration with {KeySize}-bit RSA key from {IP}", keySize, clientIp);

        // 3. Check if registration code is required/valid
        var policies = await _policyService.GetPoliciesAsync("enrollment");
        var codeRequired = policies.Any(p => p.Action == "registration_code_required" && p.Active);
        
        if (codeRequired && string.IsNullOrEmpty(request.RegistrationCode))
        {
            return BadRequest(new SecureRegisterResponse
            {
                Status = false,
                Error = "Registration code is required"
            });
        }

        if (!string.IsNullOrEmpty(request.RegistrationCode))
        {
            var isCodeValid = await ValidateRegistrationCodeAsync(request.RegistrationCode);
            if (!isCodeValid)
            {
                await _auditService.LogAsync("secure_register", false, request.User ?? "unknown",
                    request.Realm, null, request.Type, "Invalid registration code");

                return BadRequest(new SecureRegisterResponse
                {
                    Status = false,
                    Error = "Invalid registration code"
                });
            }
        }

        // 4. Generate TOTP/HOTP seed (20 bytes for SHA1, 32 for SHA256, 64 for SHA512)
        var seedLength = request.Algorithm.ToLower() switch
        {
            "sha256" => 32,
            "sha512" => 64,
            _ => 20 // sha1 default
        };
        var seed = _cryptoService.GenerateRandomBytes(seedLength);

        // 5. Generate token serial
        var tokenType = request.Type?.ToUpperInvariant() ?? "TOTP";
        var serial = await GenerateSerialAsync(tokenType);

        // 6. Encrypt seed with client's RSA public key
        byte[] encryptedSeed;
        try
        {
            encryptedSeed = _cryptoService.RsaEncryptOaep(seed, publicKeyBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt seed with RSA public key");
            
            await _auditService.LogAsync("secure_register", false, request.User ?? "unknown",
                request.Realm, serial, request.Type, "Failed to encrypt seed");

            return StatusCode(500, new SecureRegisterResponse
            {
                Status = false,
                Error = "Internal error during encryption"
            });
        }

        // 7. Create token in database
        var result = await _tokenService.InitTokenAsync(new Core.Interfaces.TokenInitRequest
        {
            Type = tokenType,
            Serial = serial,
            User = request.User,
            Realm = request.Realm,
            OtpKey = Convert.ToHexString(seed).ToLower(),
            GenerateKey = false, // We provide the key
            Description = request.Description ?? "Secure registered token",
            HashAlgorithm = request.Algorithm,
            OtpDigits = request.Digits,
            TimeStep = request.Period
        });

        if (!result.Success)
        {
            _logger.LogError("Failed to create token: {Error}", result.Message);
            
            await _auditService.LogAsync("secure_register", false, request.User ?? "unknown",
                request.Realm, serial, request.Type, result.Message);

            return BadRequest(new SecureRegisterResponse
            {
                Status = false,
                Serial = serial,
                Error = result.Message
            });
        }

        // 8. Handle device binding (optional)
        string? deviceBindingId = null;
        if (!string.IsNullOrEmpty(request.DeviceId))
        {
            deviceBindingId = Guid.NewGuid().ToString("N")[..16];
            // TODO: Store device binding in database
            _logger.LogInformation("Device binding created: {BindingId} for serial {Serial}", 
                deviceBindingId, serial);
        }

        // 9. Audit log
        await _auditService.LogAsync("secure_register", true, request.User ?? "unknown",
            request.Realm, serial, request.Type, 
            $"Secure registration successful. Key size: {keySize}. Device: {request.DeviceId ?? "none"}");

        _logger.LogInformation("Secure token registration successful: {Serial} for user {User}", 
            serial, request.User ?? "unknown");

        // 10. Return encrypted seed
        return Ok(new SecureRegisterResponse
        {
            Status = true,
            Serial = serial,
            Type = tokenType.ToLower(),
            EncryptedSeed = Convert.ToBase64String(encryptedSeed),
            Algorithm = request.Algorithm.ToLower(),
            Digits = request.Digits,
            Period = request.Period,
            Issuer = "PrivacyIDEA",
            DeviceBindingId = deviceBindingId
        });
    }

    /// <summary>
    /// GET /register/secure/info - Get secure registration capabilities
    /// </summary>
    [HttpGet("secure/info")]
    [AllowAnonymous]
    public IActionResult GetSecureRegistrationInfo()
    {
        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = new
                {
                    enabled = true,
                    min_key_size = 2048,
                    max_key_size = 4096,
                    supported_key_formats = new[] { "PKCS#1", "SPKI/X.509", "PEM" },
                    encryption_algorithm = "RSA-OAEP-SHA256",
                    allowed_types = new[] { "totp", "hotp" },
                    allowed_algorithms = new[] { "sha1", "sha256", "sha512" },
                    allowed_digits = new[] { 6, 8 },
                    device_binding = true
                }
            }
        });
    }

    private Task<bool> ValidateRegistrationCodeAsync(string code)
    {
        // TODO: Implement actual registration code validation
        // This would check against a database of valid registration codes
        return Task.FromResult(!string.IsNullOrEmpty(code) && code.Length >= 6);
    }

    private Task<string> GenerateSerialAsync(string tokenType)
    {
        var prefix = tokenType switch
        {
            "TOTP" => "TOTP",
            "HOTP" => "HOTP",
            "PUSH" => "PISH",
            "EMAIL" => "EMTP",
            "SMS" => "SMSP",
            _ => "SREG"
        };
        var random = new Random();
        var serial = $"{prefix}{random.Next(10000000, 99999999):X8}";
        return Task.FromResult(serial);
    }
}

public class RegisterRequest
{
    public string? User { get; set; }
    public string? Realm { get; set; }
    public string? Type { get; set; }
    public string? RegistrationCode { get; set; }
    public string? Description { get; set; }
}
