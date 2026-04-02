using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Event Handler Management Controller
/// Maps to Python: privacyidea/api/event.py
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "Admin")]
public class EventController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<EventController> _logger;

    public EventController(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<EventController> logger)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /event - List all event handlers
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListEventHandlers()
    {
        try
        {
            var handlers = await _unitOfWork.Repository<Domain.Entities.EventHandler>().GetAllAsync();

            var result = handlers.Select(h => new
            {
                id = h.Id,
                name = h.Name,
                active = h.Active,
                ordering = h.Ordering,
                position = h.Position,
                handlermodule = h.HandlerModule,
                events = h.Event?.Split(','),
                conditions = h.Conditions.Select(c => new
                {
                    key = c.Key,
                    value = c.Value,
                    comparator = c.Comparator
                }),
                options = h.Options.Select(o => new
                {
                    key = o.Key,
                    value = o.Value
                })
            });

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = result },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing event handlers");
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// GET /event/{id} - Get event handler by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEventHandler(int id)
    {
        try
        {
            var handler = await _unitOfWork.Repository<Domain.Entities.EventHandler>().GetByIdAsync(id);
            
            if (handler == null)
            {
                return NotFound(new { result = new { status = false }, detail = new { message = $"Event handler {id} not found" } });
            }

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new
                {
                    status = true,
                    value = new
                    {
                        id = handler.Id,
                        name = handler.Name,
                        active = handler.Active,
                        ordering = handler.Ordering,
                        position = handler.Position,
                        handlermodule = handler.HandlerModule,
                        events = handler.Event?.Split(','),
                        conditions = handler.Conditions.Select(c => new
                        {
                            key = c.Key,
                            value = c.Value,
                            comparator = c.Comparator
                        }),
                        options = handler.Options.Select(o => new
                        {
                            key = o.Key,
                            value = o.Value
                        })
                    }
                },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event handler {Id}", id);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// POST /event - Create event handler
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateEventHandler([FromBody] CreateEventHandlerRequest request)
    {
        try
        {
            var handler = new Domain.Entities.EventHandler
            {
                Name = request.Name ?? "New Handler",
                Active = request.Active ?? true,
                Ordering = request.Ordering ?? 0,
                Position = request.Position ?? "post",
                HandlerModule = request.HandlerModule ?? "UserNotification",
                Event = request.Events != null ? string.Join(",", request.Events) : "validate_check"
            };

            await _unitOfWork.Repository<Domain.Entities.EventHandler>().AddAsync(handler);
            await _unitOfWork.SaveChangesAsync();

            // Add conditions
            if (request.Conditions != null)
            {
                foreach (var cond in request.Conditions)
                {
                    var condition = new EventHandlerCondition
                    {
                        EventHandlerId = handler.Id,
                        Key = cond.Key,
                        Value = cond.Value,
                        Comparator = cond.Comparator
                    };
                    await _unitOfWork.Repository<EventHandlerCondition>().AddAsync(condition);
                }
            }

            // Add options
            if (request.Options != null)
            {
                foreach (var opt in request.Options)
                {
                    var option = new EventHandlerOption
                    {
                        EventHandlerId = handler.Id,
                        Key = opt.Key,
                        Value = opt.Value
                    };
                    await _unitOfWork.Repository<EventHandlerOption>().AddAsync(option);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync("EVENT_CREATE", true, User.Identity?.Name, info: $"Created event handler {request.Name}");

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = handler.Id },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event handler");
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// PUT /event/{id} - Update event handler
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEventHandler(int id, [FromBody] UpdateEventHandlerRequest request)
    {
        try
        {
            var handler = await _unitOfWork.Repository<Domain.Entities.EventHandler>().GetByIdAsync(id);
            
            if (handler == null)
            {
                return NotFound(new { result = new { status = false }, detail = new { message = $"Event handler {id} not found" } });
            }

            if (request.Name != null) handler.Name = request.Name;
            if (request.Active.HasValue) handler.Active = request.Active.Value;
            if (request.Ordering.HasValue) handler.Ordering = request.Ordering.Value;
            if (request.Position != null) handler.Position = request.Position;
            if (request.HandlerModule != null) handler.HandlerModule = request.HandlerModule;
            if (request.Events != null) handler.Event = string.Join(",", request.Events);

            _unitOfWork.Repository<Domain.Entities.EventHandler>().Update(handler);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync("EVENT_UPDATE", true, User.Identity?.Name, info: $"Updated event handler {id}");

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = handler.Id },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event handler {Id}", id);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// DELETE /event/{id} - Delete event handler
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEventHandler(int id)
    {
        try
        {
            var handler = await _unitOfWork.Repository<Domain.Entities.EventHandler>().GetByIdAsync(id);
            
            if (handler == null)
            {
                return NotFound(new { result = new { status = false }, detail = new { message = $"Event handler {id} not found" } });
            }

            // Delete conditions
            foreach (var cond in handler.Conditions.ToList())
            {
                _unitOfWork.Repository<EventHandlerCondition>().Remove(cond);
            }

            // Delete options
            foreach (var opt in handler.Options.ToList())
            {
                _unitOfWork.Repository<EventHandlerOption>().Remove(opt);
            }

            _unitOfWork.Repository<Domain.Entities.EventHandler>().Remove(handler);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync("EVENT_DELETE", true, User.Identity?.Name, info: $"Deleted event handler {id}");

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = id },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event handler {Id}", id);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// POST /event/enable/{id} - Enable event handler
    /// </summary>
    [HttpPost("enable/{id:int}")]
    public async Task<IActionResult> EnableEventHandler(int id)
    {
        try
        {
            var handler = await _unitOfWork.Repository<Domain.Entities.EventHandler>().GetByIdAsync(id);
            
            if (handler == null)
            {
                return NotFound(new { result = new { status = false }, detail = new { message = $"Event handler {id} not found" } });
            }

            handler.Active = true;
            _unitOfWork.Repository<Domain.Entities.EventHandler>().Update(handler);
            await _unitOfWork.SaveChangesAsync();

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
            _logger.LogError(ex, "Error enabling event handler {Id}", id);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// POST /event/disable/{id} - Disable event handler
    /// </summary>
    [HttpPost("disable/{id:int}")]
    public async Task<IActionResult> DisableEventHandler(int id)
    {
        try
        {
            var handler = await _unitOfWork.Repository<Domain.Entities.EventHandler>().GetByIdAsync(id);
            
            if (handler == null)
            {
                return NotFound(new { result = new { status = false }, detail = new { message = $"Event handler {id} not found" } });
            }

            handler.Active = false;
            _unitOfWork.Repository<Domain.Entities.EventHandler>().Update(handler);
            await _unitOfWork.SaveChangesAsync();

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
            _logger.LogError(ex, "Error disabling event handler {Id}", id);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// GET /event/available - Get available events
    /// </summary>
    [HttpGet("available")]
    public IActionResult GetAvailableEvents()
    {
        var events = new[]
        {
            "validate_check", "validate_triggerchallenge",
            "token_init", "token_assign", "token_unassign", "token_delete",
            "token_enable", "token_disable", "token_setpin", "token_resync",
            "user_add", "user_update", "user_delete",
            "admin_login", "admin_logout",
            "policy_set", "policy_delete",
            "resolver_add", "resolver_delete",
            "realm_add", "realm_delete",
            "pre_check", "post_check"
        };

        return Ok(new
        {
            id = 1,
            jsonrpc = "2.0",
            result = new { status = true, value = events },
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            version = "1.0.0"
        });
    }

    /// <summary>
    /// GET /event/actions - Get available handler actions
    /// </summary>
    [HttpGet("actions")]
    public IActionResult GetAvailableActions()
    {
        var actions = new Dictionary<string, string[]>
        {
            ["UserNotification"] = new[] { "sendemail", "sendsms" },
            ["Logging"] = new[] { "log_info", "log_warning", "log_error" },
            ["Counter"] = new[] { "increase_counter", "decrease_counter", "reset_counter" },
            ["Token"] = new[] { "enable", "disable", "delete", "unassign", "set_validity" },
            ["Script"] = new[] { "run_script" },
            ["Webhook"] = new[] { "post_to_webhook" }
        };

        return Ok(new
        {
            id = 1,
            jsonrpc = "2.0",
            result = new { status = true, value = actions },
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            version = "1.0.0"
        });
    }

    /// <summary>
    /// GET /event/conditions - Get available conditions
    /// </summary>
    [HttpGet("conditions")]
    public IActionResult GetAvailableConditions()
    {
        var conditions = new Dictionary<string, object>
        {
            ["userinfo"] = new { keys = new[] { "username", "email", "givenname", "surname" } },
            ["tokeninfo"] = new { keys = new[] { "serial", "type", "description" } },
            ["request"] = new { keys = new[] { "client_ip", "user_agent" } },
            ["response"] = new { keys = new[] { "result.value", "result.status" } }
        };

        return Ok(new
        {
            id = 1,
            jsonrpc = "2.0",
            result = new { status = true, value = conditions },
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            version = "1.0.0"
        });
    }
}

#region Request Models

public class CreateEventHandlerRequest
{
    public string Name { get; set; } = string.Empty;
    public bool? Active { get; set; }
    public int? Ordering { get; set; }
    public string? Position { get; set; }
    public string? HandlerModule { get; set; }
    public string[]? Events { get; set; }
    public string? Action { get; set; }
    public List<EventConditionRequest>? Conditions { get; set; }
    public List<EventOptionRequest>? Options { get; set; }
}

public class UpdateEventHandlerRequest
{
    public string? Name { get; set; }
    public bool? Active { get; set; }
    public int? Ordering { get; set; }
    public string? Position { get; set; }
    public string? HandlerModule { get; set; }
    public string[]? Events { get; set; }
    public string? Action { get; set; }
}

public class EventConditionRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string Comparator { get; set; } = "equals";
}

public class EventOptionRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
}

#endregion
