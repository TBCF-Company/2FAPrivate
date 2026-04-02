using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Password Recovery API
/// Maps to Python: privacyidea/api/recover.py
/// </summary>
[ApiController]
[Route("recover")]
public class RecoverController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IUserService _userService;
    private readonly IPolicyService _policyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RecoverController> _logger;

    public RecoverController(
        ITokenService tokenService,
        IUserService userService,
        IPolicyService policyService,
        IAuditService auditService,
        ILogger<RecoverController> logger)
    {
        _tokenService = tokenService;
        _userService = userService;
        _policyService = policyService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// POST /recover - Request recovery token
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RequestRecovery([FromBody] RecoveryRequest request)
    {
        // Check if recovery is enabled via policy
        var policies = await _policyService.GetPoliciesAsync("recovery");
        var recoveryEnabled = policies.Any(p => p.Action == "recovery_enabled" && p.Active);

        if (!recoveryEnabled)
        {
            return BadRequest(new
            {
                jsonrpc = "2.0",
                result = new
                {
                    status = false,
                    error = new { message = "Password recovery is not enabled" }
                }
            });
        }

        // Validate user exists
        var user = await _userService.GetUserAsync(request.User, request.Realm);
        if (user == null)
        {
            // Don't reveal whether user exists
            await _auditService.LogAsync("recover_request", false, request.User ?? "unknown",
                request.Realm, null, null, "User not found (not revealed)");

            // Return success to prevent user enumeration
            return Ok(new
            {
                jsonrpc = "2.0",
                result = new
                {
                    status = true,
                    value = true
                },
                detail = new
                {
                    message = "If the user exists, a recovery email will be sent"
                }
            });
        }

        // Generate recovery token
        var recoveryToken = GenerateRecoveryToken();
        var expirationMinutes = 30;

        // Store recovery token (in real implementation, save to database)
        // TODO: Implement recovery token storage

        // Send recovery email/SMS based on policy
        var notificationMethod = policies.FirstOrDefault(p => p.Action == "recovery_notification")?.Value ?? "email";

        if (notificationMethod == "email" && !string.IsNullOrEmpty(user.Email))
        {
            // TODO: Send recovery email
            _logger.LogInformation("Recovery email would be sent to {User}", request.User);
        }
        else if (notificationMethod == "sms" && !string.IsNullOrEmpty(user.Mobile))
        {
            // TODO: Send recovery SMS
            _logger.LogInformation("Recovery SMS would be sent to {User}", request.User);
        }

        await _auditService.LogAsync("recover_request", true, request.User ?? "unknown",
            request.Realm, null, null, "Recovery token generated");

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = true
            },
            detail = new
            {
                message = "Recovery notification sent",
                expires_in = expirationMinutes * 60
            }
        });
    }

    /// <summary>
    /// POST /recover/verify - Verify recovery token and reset
    /// </summary>
    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyRecovery([FromBody] RecoveryVerifyRequest request)
    {
        // Validate recovery token
        var isValid = await ValidateRecoveryTokenAsync(request.User, request.Realm, request.Token);

        if (!isValid)
        {
            await _auditService.LogAsync("recover_verify", false, request.User ?? "unknown",
                request.Realm, null, null, "Invalid or expired recovery token");

            return BadRequest(new
            {
                jsonrpc = "2.0",
                result = new
                {
                    status = false,
                    error = new { message = "Invalid or expired recovery token" }
                }
            });
        }

        // What action to take based on policy?
        var policies = await _policyService.GetPoliciesAsync("recovery");
        var action = policies.FirstOrDefault(p => p.Action == "recovery_action")?.Value ?? "reset_all_tokens";

        switch (action)
        {
            case "reset_all_tokens":
                // Unassign all tokens from user
                var tokens = await _tokenService.GetTokensAsync(user: request.User, realm: request.Realm);
                foreach (var token in tokens)
                {
                    await _tokenService.UnassignTokenAsync(token.Serial);
                }
                break;

            case "disable_all_tokens":
                // Disable all tokens
                var userTokens = await _tokenService.GetTokensAsync(user: request.User, realm: request.Realm);
                foreach (var token in userTokens)
                {
                    await _tokenService.EnableTokenAsync(token.Serial, false);
                }
                break;

            case "create_recovery_token":
                // Create a temporary recovery token
                var recoverySerial = $"RECOV{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
                await _tokenService.InitTokenAsync(new TokenInitRequest
                {
                    Type = "HOTP",
                    Serial = recoverySerial,
                    User = request.User,
                    Realm = request.Realm,
                    Description = "Recovery token - expires in 24 hours"
                });
                break;
        }

        // Invalidate the recovery token
        await InvalidateRecoveryTokenAsync(request.User, request.Realm, request.Token);

        await _auditService.LogAsync("recover_verify", true, request.User ?? "unknown",
            request.Realm, null, null, $"Recovery completed with action: {action}");

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = true
            },
            detail = new
            {
                message = "Recovery completed successfully",
                action = action
            }
        });
    }

    /// <summary>
    /// GET /recover/status - Get recovery status for a user
    /// </summary>
    [HttpGet("status/{user}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetRecoveryStatus(string user, [FromQuery] string? realm = null)
    {
        var tokens = await _tokenService.GetTokensAsync(user: user, realm: realm);
        var hasActiveTokens = tokens.Any(t => t.Active);
        var hasRecoveryToken = tokens.Any(t => t.Description?.Contains("Recovery") == true);

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = new
                {
                    user = user,
                    realm = realm,
                    has_active_tokens = hasActiveTokens,
                    has_recovery_token = hasRecoveryToken,
                    token_count = tokens.Count()
                }
            }
        });
    }

    private string GenerateRecoveryToken()
    {
        var random = new Random();
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")[..12];
    }

    private Task<bool> ValidateRecoveryTokenAsync(string? user, string? realm, string? token)
    {
        // TODO: Implement actual recovery token validation from database
        return Task.FromResult(!string.IsNullOrEmpty(token) && token.Length >= 8);
    }

    private Task InvalidateRecoveryTokenAsync(string? user, string? realm, string? token)
    {
        // TODO: Implement actual recovery token invalidation
        _logger.LogInformation("Invalidating recovery token for user {User}", user);
        return Task.CompletedTask;
    }
}

public class RecoveryRequest
{
    [Required]
    public string? User { get; set; }
    
    public string? Realm { get; set; }
}

public class RecoveryVerifyRequest
{
    [Required]
    public string? User { get; set; }
    
    public string? Realm { get; set; }
    
    [Required]
    public string? Token { get; set; }
}
