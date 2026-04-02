using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Api.Models;
using PrivacyIDEA.Domain.Entities;
using PrivacyIDEA.Infrastructure.Data;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Token API - Token management endpoints
/// Maps to Python: privacyidea/api/token.py
/// </summary>
[ApiController]
[Route("[controller]")]
public class TokenController : ControllerBase
{
    private readonly PrivacyIdeaDbContext _context;
    private readonly ILogger<TokenController> _logger;

    public TokenController(PrivacyIdeaDbContext context, ILogger<TokenController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// List tokens
    /// GET /token/
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTokens(
        [FromQuery] string? serial = null,
        [FromQuery] string? type = null,
        [FromQuery] string? user = null,
        [FromQuery] string? realm = null,
        [FromQuery] bool? active = null,
        [FromQuery] int page = 1,
        [FromQuery] int pagesize = 15)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var query = _context.Tokens
                .Include(t => t.TokenInfos)
                .Include(t => t.TokenOwners)
                    .ThenInclude(o => o.Realm)
                .Include(t => t.TokenRealms)
                    .ThenInclude(tr => tr.Realm)
                .AsQueryable();

            if (!string.IsNullOrEmpty(serial))
                query = query.Where(t => t.Serial.Contains(serial));
            
            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.TokenType == type);
            
            if (active.HasValue)
                query = query.Where(t => t.Active == active.Value);

            var totalCount = await query.CountAsync();
            var tokens = await query
                .Skip((page - 1) * pagesize)
                .Take(pagesize)
                .ToListAsync();

            var tokenDtos = tokens.Select(t => new TokenDto
            {
                Id = t.Id,
                Serial = t.Serial,
                TokenType = t.TokenType,
                Description = t.Description,
                Active = t.Active,
                Revoked = t.Revoked,
                Locked = t.Locked,
                FailCount = t.FailCount,
                MaxFail = t.MaxFail,
                OtpLen = t.OtpLen,
                Count = t.Count,
                RolloutState = t.RolloutState,
                Info = t.TokenInfos?.ToDictionary(i => i.Key, i => i.Value ?? ""),
                Owners = t.TokenOwners?.Select(o => new TokenOwnerDto
                {
                    UserId = o.UserId,
                    Resolver = o.Resolver,
                    Realm = o.Realm?.Name
                }),
                Realms = t.TokenRealms?.Select(tr => tr.Realm?.Name ?? "")
            });

            return Ok(new ApiResponse<TokenListResponse>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<TokenListResponse>
                {
                    Status = true,
                    Value = new TokenListResponse
                    {
                        Count = totalCount,
                        Current = page,
                        Tokens = tokenDtos
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tokens");
            return Ok(new ApiResponse<TokenListResponse>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<TokenListResponse>
                {
                    Status = false,
                    Error = new ErrorInfo { Code = 500, Message = ex.Message }
                }
            });
        }
    }

    /// <summary>
    /// Initialize/Create a new token
    /// POST /token/init
    /// </summary>
    [HttpPost("init")]
    public async Task<IActionResult> InitToken([FromForm] TokenInitRequest request)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var serial = request.Serial ?? GenerateSerial(request.Type);

            var token = new Token
            {
                Serial = serial,
                TokenType = request.Type.ToLower(),
                Description = request.Description ?? "",
                OtpLen = request.OtpLen ?? 6,
                Active = true
            };

            // Generate secret key if requested
            if (request.GenKey == "1" || string.IsNullOrEmpty(request.OtpKey))
            {
                // TODO: Generate and encrypt secret key
            }

            _context.Tokens.Add(token);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Token created: {Serial}", serial);

            return Ok(new ApiResponse<TokenDto>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<TokenDto>
                {
                    Status = true,
                    Value = new TokenDto
                    {
                        Id = token.Id,
                        Serial = token.Serial,
                        TokenType = token.TokenType,
                        Description = token.Description,
                        Active = token.Active,
                        OtpLen = token.OtpLen
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating token");
            return Ok(new ApiResponse<TokenDto>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<TokenDto>
                {
                    Status = false,
                    Error = new ErrorInfo { Code = 500, Message = ex.Message }
                }
            });
        }
    }

    /// <summary>
    /// Delete a token
    /// DELETE /token/{serial}
    /// </summary>
    [HttpDelete("{serial}")]
    public async Task<IActionResult> DeleteToken(string serial)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => t.Serial == serial);
            if (token == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Time = (DateTime.UtcNow - startTime).TotalSeconds,
                    Result = new ResultWrapper<object>
                    {
                        Status = false,
                        Error = new ErrorInfo { Code = 404, Message = $"Token {serial} not found" }
                    }
                });
            }

            _context.Tokens.Remove(token);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Token deleted: {Serial}", serial);

            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = true,
                    Value = new { serial }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting token {Serial}", serial);
            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = false,
                    Error = new ErrorInfo { Code = 500, Message = ex.Message }
                }
            });
        }
    }

    /// <summary>
    /// Enable a token
    /// POST /token/enable
    /// </summary>
    [HttpPost("enable")]
    public async Task<IActionResult> EnableToken([FromForm] string serial)
    {
        return await SetTokenActive(serial, true);
    }

    /// <summary>
    /// Disable a token
    /// POST /token/disable
    /// </summary>
    [HttpPost("disable")]
    public async Task<IActionResult> DisableToken([FromForm] string serial)
    {
        return await SetTokenActive(serial, false);
    }

    private async Task<IActionResult> SetTokenActive(string serial, bool active)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => t.Serial == serial);
            if (token == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Time = (DateTime.UtcNow - startTime).TotalSeconds,
                    Result = new ResultWrapper<object>
                    {
                        Status = false,
                        Error = new ErrorInfo { Code = 404, Message = $"Token {serial} not found" }
                    }
                });
            }

            token.Active = active;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = true,
                    Value = new { serial, active }
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = false,
                    Error = new ErrorInfo { Code = 500, Message = ex.Message }
                }
            });
        }
    }

    /// <summary>
    /// Reset token fail counter
    /// POST /token/reset
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetFailCounter([FromForm] string serial)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => t.Serial == serial);
            if (token == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Time = (DateTime.UtcNow - startTime).TotalSeconds,
                    Result = new ResultWrapper<object>
                    {
                        Status = false,
                        Error = new ErrorInfo { Code = 404, Message = $"Token {serial} not found" }
                    }
                });
            }

            token.FailCount = 0;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = true,
                    Value = new { serial, failcount = 0 }
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = false,
                    Error = new ErrorInfo { Code = 500, Message = ex.Message }
                }
            });
        }
    }

    private static string GenerateSerial(string tokenType)
    {
        var prefix = tokenType.ToUpper() switch
        {
            "HOTP" => "OATH",
            "TOTP" => "TOTP",
            "SMS" => "SMSTOKEN",
            "EMAIL" => "EMAIL",
            "PUSH" => "PIPU",
            "WEBAUTHN" => "WAN",
            "PASSKEY" => "PSKK",
            _ => tokenType.ToUpper()
        };
        
        return $"{prefix}{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
