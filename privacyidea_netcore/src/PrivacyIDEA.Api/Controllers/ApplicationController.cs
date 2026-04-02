using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Application Settings API
/// Maps to Python: privacyidea/api/application.py and config-related endpoints
/// </summary>
[ApiController]
[Route("application")]
[Authorize(Policy = "Admin")]
public class ApplicationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IPolicyService _policyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ApplicationController> _logger;

    public ApplicationController(
        IConfiguration configuration,
        IPolicyService policyService,
        IAuditService auditService,
        ILogger<ApplicationController> logger)
    {
        _configuration = configuration;
        _policyService = policyService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /application/ - Get application configuration
    /// </summary>
    [HttpGet]
    public IActionResult GetConfiguration()
    {
        var config = new
        {
            app_name = _configuration["PrivacyIDEA:AppName"] ?? "PrivacyIDEA",
            version = "4.0.0",
            mode = _configuration["PrivacyIDEA:Mode"] ?? "production",
            default_realm = _configuration["PrivacyIDEA:DefaultRealm"],
            enable_audit = _configuration.GetValue<bool>("PrivacyIDEA:EnableAudit", true),
            enable_policies = _configuration.GetValue<bool>("PrivacyIDEA:EnablePolicies", true),
            logout_time = _configuration.GetValue<int>("PrivacyIDEA:LogoutTime", 120)
        };

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = config
            }
        });
    }

    /// <summary>
    /// GET /application/tokens - Get available token types
    /// </summary>
    [HttpGet("tokens")]
    [AllowAnonymous]
    public IActionResult GetTokenTypes()
    {
        var tokenTypes = new[]
        {
            new { type = "hotp", title = "HOTP", desc = "Event-based One Time Password" },
            new { type = "totp", title = "TOTP", desc = "Time-based One Time Password" },
            new { type = "push", title = "Push", desc = "Smartphone push notification" },
            new { type = "webauthn", title = "WebAuthn", desc = "FIDO2/Passkey authentication" },
            new { type = "sms", title = "SMS", desc = "SMS One Time Password" },
            new { type = "email", title = "Email", desc = "Email One Time Password" },
            new { type = "registration", title = "Registration", desc = "Registration code token" },
            new { type = "certificate", title = "Certificate", desc = "X.509 certificate" },
            new { type = "ssh", title = "SSH", desc = "SSH key authentication" },
            new { type = "yubico", title = "Yubico", desc = "Yubico YubiKey" },
            new { type = "yubikey", title = "Yubikey AES", desc = "Yubikey with AES mode" },
            new { type = "radius", title = "RADIUS", desc = "Forward to RADIUS server" },
            new { type = "password", title = "Password", desc = "Static password token" },
            new { type = "daplug", title = "Daplug", desc = "Daplug OTP token" },
            new { type = "motp", title = "mOTP", desc = "Mobile OTP" },
            new { type = "paper", title = "Paper/TAN", desc = "Paper TAN list" },
            new { type = "spass", title = "SPass", desc = "Simple Pass token" },
            new { type = "question", title = "Question", desc = "Security questions" },
            new { type = "tiqr", title = "TiQR", desc = "TiQR smartphone app" },
            new { type = "u2f", title = "U2F", desc = "FIDO U2F" },
            new { type = "vasco", title = "VASCO", desc = "VASCO DIGIPASS" },
            new { type = "daypassword", title = "DayPassword", desc = "Day-based password" },
            new { type = "foureyestoken", title = "4Eyes", desc = "Four eyes principle" },
            new { type = "remote", title = "Remote", desc = "Forward to remote server" },
            new { type = "tan", title = "TAN", desc = "Transaction number" }
        };

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = tokenTypes
            }
        });
    }

    /// <summary>
    /// GET /application/rights - Get rights for token types
    /// </summary>
    [HttpGet("rights")]
    public async Task<IActionResult> GetRights()
    {
        var policies = await _policyService.GetPoliciesAsync("admin");

        var rights = new Dictionary<string, object>
        {
            ["create"] = true,
            ["delete"] = true,
            ["enable"] = true,
            ["disable"] = true,
            ["assign"] = true,
            ["unassign"] = true,
            ["resync"] = true,
            ["reset"] = true,
            ["setpin"] = true,
            ["getserial"] = true
        };

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = rights
            }
        });
    }
}
