using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// User Controller
/// Maps to Python: privacyidea/api/user.py
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        ITokenService tokenService,
        IAuditService auditService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _tokenService = tokenService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /user - List users
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? username = null,
        [FromQuery] string? realm = null,
        [FromQuery] string? resolver = null,
        [FromQuery] string? email = null,
        [FromQuery] int page = 1,
        [FromQuery] int pagesize = 15)
    {
        try
        {
            var filter = new UserSearchFilter
            {
                Username = username,
                Realm = realm,
                Resolver = resolver,
                Email = email
            };

            var result = await _userService.SearchUsersAsync(filter, page, pagesize);

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new
                {
                    status = true,
                    value = new
                    {
                        count = result.TotalCount,
                        current = page,
                        data = result.Items.Select(u => new
                        {
                            username = u.Username,
                            userid = u.UserId,
                            realm = u.Realm,
                            resolver = u.Resolver,
                            email = u.Email,
                            givenname = u.GivenName,
                            surname = u.Surname,
                            phone = u.Phone,
                            mobile = u.Mobile,
                            description = u.Description,
                            editable = u.Editable
                        })
                    }
                },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing users");
            return StatusCode(500, new
            {
                result = new { status = false },
                error = new { message = ex.Message }
            });
        }
    }

    /// <summary>
    /// GET /user/{userid} - Get user details
    /// </summary>
    [HttpGet("{userid}")]
    public async Task<IActionResult> GetUser(string userid, [FromQuery] string? realm = null)
    {
        try
        {
            var user = await _userService.GetUserAsync(userid, realm);
            
            if (user == null)
            {
                return NotFound(new
                {
                    result = new { status = false },
                    detail = new { message = $"User '{userid}' not found" }
                });
            }

            // Get user's tokens
            var tokens = await _tokenService.GetTokensForUserAsync(userid, realm);

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new
                {
                    status = true,
                    value = new
                    {
                        username = user.Username,
                        userid = user.UserId,
                        realm = user.Realm,
                        resolver = user.Resolver,
                        email = user.Email,
                        givenname = user.GivenName,
                        surname = user.Surname,
                        phone = user.Phone,
                        mobile = user.Mobile,
                        description = user.Description,
                        editable = user.Editable,
                        tokens = tokens.Select(t => new
                        {
                            serial = t.Serial,
                            type = t.TokenType,
                            active = t.Active,
                            description = t.Description
                        }),
                        attributes = user.Attributes
                    }
                },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userid);
            return StatusCode(500, new
            {
                result = new { status = false },
                error = new { message = ex.Message }
            });
        }
    }

    /// <summary>
    /// POST /user - Create user (for editable resolvers)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var createRequest = new UserCreateRequest
            {
                Username = request.Username,
                Realm = request.Realm,
                Resolver = request.Resolver,
                Password = request.Password,
                Email = request.Email,
                GivenName = request.GivenName,
                Surname = request.Surname,
                Phone = request.Phone,
                Mobile = request.Mobile,
                Description = request.Description
            };

            var user = await _userService.CreateUserAsync(createRequest);

            await _auditService.LogAsync(
                "USER_CREATE",
                true,
                User.Identity?.Name,
                request.Realm,
                info: $"Created user {request.Username}"
            );

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new
                {
                    status = true,
                    value = new
                    {
                        username = user.Username,
                        userid = user.UserId,
                        realm = user.Realm,
                        resolver = user.Resolver
                    }
                },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                result = new { status = false },
                detail = new { message = ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Username}", request.Username);
            return StatusCode(500, new
            {
                result = new { status = false },
                error = new { message = ex.Message }
            });
        }
    }

    /// <summary>
    /// PUT /user/{userid} - Update user
    /// </summary>
    [HttpPut("{userid}")]
    public async Task<IActionResult> UpdateUser(string userid, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var updateRequest = new UserUpdateRequest
            {
                Email = request.Email,
                GivenName = request.GivenName,
                Surname = request.Surname,
                Phone = request.Phone,
                Mobile = request.Mobile,
                Description = request.Description,
                Password = request.Password
            };

            var success = await _userService.UpdateUserAsync(userid, request.Realm, updateRequest);

            if (!success)
            {
                return NotFound(new
                {
                    result = new { status = false },
                    detail = new { message = $"User '{userid}' not found or not editable" }
                });
            }

            await _auditService.LogAsync(
                "USER_UPDATE",
                true,
                User.Identity?.Name,
                request.Realm,
                info: $"Updated user {userid}"
            );

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = true },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userid);
            return StatusCode(500, new
            {
                result = new { status = false },
                error = new { message = ex.Message }
            });
        }
    }

    /// <summary>
    /// DELETE /user/{userid} - Delete user
    /// </summary>
    [HttpDelete("{userid}")]
    public async Task<IActionResult> DeleteUser(string userid, [FromQuery] string? realm = null)
    {
        try
        {
            var success = await _userService.DeleteUserAsync(userid, realm);

            if (!success)
            {
                return NotFound(new
                {
                    result = new { status = false },
                    detail = new { message = $"User '{userid}' not found or not editable" }
                });
            }

            await _auditService.LogAsync(
                "USER_DELETE",
                true,
                User.Identity?.Name,
                realm,
                info: $"Deleted user {userid}"
            );

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = true },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userid);
            return StatusCode(500, new
            {
                result = new { status = false },
                error = new { message = ex.Message }
            });
        }
    }

    /// <summary>
    /// GET /user/attribute/{userid} - Get user attributes
    /// </summary>
    [HttpGet("attribute/{userid}")]
    public async Task<IActionResult> GetUserAttributes(string userid, [FromQuery] string? realm = null)
    {
        try
        {
            var attributes = await _userService.GetUserAttributesAsync(userid, realm);

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new
                {
                    status = true,
                    value = attributes
                },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attributes for user {UserId}", userid);
            return StatusCode(500, new
            {
                result = new { status = false },
                error = new { message = ex.Message }
            });
        }
    }

    /// <summary>
    /// POST /user/attribute/{userid} - Set user attribute
    /// </summary>
    [HttpPost("attribute/{userid}")]
    public async Task<IActionResult> SetUserAttribute(
        string userid, 
        [FromBody] SetAttributeRequest request,
        [FromQuery] string? realm = null)
    {
        try
        {
            var success = await _userService.SetUserAttributeAsync(
                userid, realm, request.Key, request.Value);

            if (!success)
            {
                return NotFound(new
                {
                    result = new { status = false },
                    detail = new { message = $"User '{userid}' not found" }
                });
            }

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = true },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting attribute for user {UserId}", userid);
            return StatusCode(500, new
            {
                result = new { status = false },
                error = new { message = ex.Message }
            });
        }
    }

    /// <summary>
    /// DELETE /user/attribute/{userid}/{key} - Delete user attribute
    /// </summary>
    [HttpDelete("attribute/{userid}/{key}")]
    public async Task<IActionResult> DeleteUserAttribute(
        string userid, 
        string key,
        [FromQuery] string? realm = null)
    {
        try
        {
            var success = await _userService.DeleteUserAttributeAsync(userid, realm, key);

            if (!success)
            {
                return NotFound(new
                {
                    result = new { status = false },
                    detail = new { message = $"Attribute '{key}' not found for user '{userid}'" }
                });
            }

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = true },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attribute for user {UserId}", userid);
            return StatusCode(500, new
            {
                result = new { status = false },
                error = new { message = ex.Message }
            });
        }
    }

    /// <summary>
    /// GET /user/realms - Get available realms
    /// </summary>
    [HttpGet("realms")]
    public async Task<IActionResult> GetRealms()
    {
        try
        {
            var realms = await _userService.GetRealmsAsync();
            var defaultRealm = await _userService.GetDefaultRealmAsync();

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new
                {
                    status = true,
                    value = new
                    {
                        realms = realms,
                        default_realm = defaultRealm
                    }
                },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting realms");
            return StatusCode(500, new
            {
                result = new { status = false },
                error = new { message = ex.Message }
            });
        }
    }
}

#region Request Models

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Realm { get; set; }
    public string? Resolver { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Description { get; set; }
}

public class UpdateUserRequest
{
    public string? Realm { get; set; }
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Description { get; set; }
    public string? Password { get; set; }
}

public class SetAttributeRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

#endregion
