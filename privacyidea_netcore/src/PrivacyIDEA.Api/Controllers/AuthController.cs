using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Authentication Controller
/// Maps to Python: privacyidea/api/auth.py
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IAuditService auditService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// POST /auth - Authenticate and get JWT token
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Authenticate([FromBody] AuthRequest request)
    {
        try
        {
            AuthResult result;

            if (request.Username != null && request.Password != null)
            {
                // Try admin authentication first
                result = await _authService.AuthenticateAdminAsync(request.Username, request.Password);

                if (!result.Success && request.Realm != null)
                {
                    // Try user authentication
                    result = await _authService.AuthenticateUserAsync(
                        request.Username, 
                        request.Realm, 
                        request.Password, 
                        request.Otp);
                }
            }
            else
            {
                return BadRequest(new AuthResponse
                {
                    Result = new AuthResultData { Status = false, Value = false },
                    Detail = new AuthDetail { Message = "Username and password required" }
                });
            }

            await _auditService.LogAsync(
                result.Success ? "AUTH_SUCCESS" : "AUTH_FAILED",
                result.Success,
                request.Username,
                request.Realm,
                info: "POST /auth"
            );

            if (result.Success)
            {
                return Ok(new AuthResponse
                {
                    Result = new AuthResultData
                    {
                        Status = true,
                        Value = new AuthTokenResponse
                        {
                            Token = result.Token!,
                            RefreshToken = result.RefreshToken,
                            Username = result.Username!,
                            Realm = result.Realm,
                            Role = result.Role!,
                            Rights = result.Rights
                        }
                    }
                });
            }

            if (result.RequiresSecondFactor)
            {
                return Ok(new AuthResponse
                {
                    Result = new AuthResultData { Status = true, Value = false },
                    Detail = new AuthDetail
                    {
                        Message = "Second factor required",
                        TransactionId = result.TransactionId,
                        Attributes = new { multi_challenge = true }
                    }
                });
            }

            return Unauthorized(new AuthResponse
            {
                Result = new AuthResultData { Status = false, Value = false },
                Detail = new AuthDetail { Message = result.ErrorMessage ?? "Authentication failed" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error for user {Username}", request.Username);
            return StatusCode(500, new AuthResponse
            {
                Result = new AuthResultData { Status = false, Value = false },
                Detail = new AuthDetail { Message = "Internal server error" }
            });
        }
    }

    /// <summary>
    /// GET /auth - Get current authentication info
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAuthInfo()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized(new { error = "Not authenticated" });
        }

        var rights = await _authService.GetUserRightsAsync(username);

        return Ok(new
        {
            result = new
            {
                status = true,
                value = new
                {
                    username = username,
                    role = rights.Role,
                    realm = rights.Realm,
                    rights = rights.Permissions,
                    is_admin = rights.IsAdmin
                }
            }
        });
    }

    /// <summary>
    /// DELETE /auth - Logout/revoke token
    /// </summary>
    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        
        if (!string.IsNullOrEmpty(token))
        {
            await _authService.RevokeTokenAsync(token);
        }

        await _auditService.LogAsync(
            "AUTH_LOGOUT",
            true,
            User.Identity?.Name,
            info: "DELETE /auth"
        );

        return Ok(new
        {
            result = new { status = true, value = true }
        });
    }

    /// <summary>
    /// GET /auth/rights - Get user rights
    /// </summary>
    [HttpGet("rights")]
    [Authorize]
    public async Task<IActionResult> GetRights()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized(new { error = "Not authenticated" });
        }

        var realm = User.FindFirst("realm")?.Value;
        var rights = await _authService.GetUserRightsAsync(username, realm);

        return Ok(new
        {
            result = new
            {
                status = true,
                value = new
                {
                    realms = rights.Realms,
                    rights = rights.Permissions,
                    policies = rights.PolicyRights
                }
            }
        });
    }

    /// <summary>
    /// POST /auth/refresh - Refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (result.Success)
        {
            return Ok(new AuthResponse
            {
                Result = new AuthResultData
                {
                    Status = true,
                    Value = new AuthTokenResponse
                    {
                        Token = result.Token!,
                        RefreshToken = result.RefreshToken
                    }
                }
            });
        }

        return Unauthorized(new AuthResponse
        {
            Result = new AuthResultData { Status = false, Value = false },
            Detail = new AuthDetail { Message = result.ErrorMessage ?? "Invalid refresh token" }
        });
    }
}

#region Request/Response Models

public class AuthRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Realm { get; set; }
    public string? Otp { get; set; }
    public string? TransactionId { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public int Id { get; set; } = 1;
    public string JsonRpc { get; set; } = "2.0";
    public AuthResultData Result { get; set; } = new();
    public double Time { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public string Version { get; set; } = "1.0.0";
    public AuthDetail? Detail { get; set; }
}

public class AuthResultData
{
    public bool Status { get; set; }
    public object Value { get; set; } = false;
}

public class AuthTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? Username { get; set; }
    public string? Realm { get; set; }
    public string? Role { get; set; }
    public List<string>? Rights { get; set; }
}

public class AuthDetail
{
    public string? Message { get; set; }
    public string? TransactionId { get; set; }
    public object? Attributes { get; set; }
}

#endregion
