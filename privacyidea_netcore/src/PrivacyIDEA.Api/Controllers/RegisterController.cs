using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<RegisterController> _logger;

    public RegisterController(
        ITokenService tokenService,
        IPolicyService policyService,
        IAuditService auditService,
        ILogger<RegisterController> logger)
    {
        _tokenService = tokenService;
        _policyService = policyService;
        _auditService = auditService;
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
        var result = await _tokenService.InitTokenAsync(new TokenInitRequest
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
