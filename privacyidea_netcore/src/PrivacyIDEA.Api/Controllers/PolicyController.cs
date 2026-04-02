using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Policy Management Controller
/// Maps to Python: privacyidea/api/policy.py
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "Admin")]
public class PolicyController : ControllerBase
{
    private readonly IPolicyService _policyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PolicyController> _logger;

    public PolicyController(
        IPolicyService policyService,
        IAuditService auditService,
        ILogger<PolicyController> logger)
    {
        _policyService = policyService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /policy - List all policies
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListPolicies(
        [FromQuery] string? scope = null,
        [FromQuery] string? name = null,
        [FromQuery] bool? active = null)
    {
        try
        {
            var policies = await _policyService.GetPoliciesAsync(scope, active);

            if (!string.IsNullOrEmpty(name))
            {
                policies = policies.Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            }

            var result = policies.Select(p => new
            {
                name = p.Name,
                scope = p.Scope,
                active = p.Active,
                action = p.Action,
                realm = p.Realm,
                resolver = p.Resolver,
                user = p.User,
                client = p.Client,
                time = p.Time,
                admin_realm = p.AdminRealm,
                priority = p.Priority,
                check_all_resolvers = p.CheckAllResolvers,
                conditions = p.Conditions.Select(c => new
                {
                    section = c.Section,
                    key = c.Key,
                    comparator = c.Comparator,
                    value = c.Value,
                    active = c.Active
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
            _logger.LogError(ex, "Error listing policies");
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// GET /policy/{name} - Get policy by name
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetPolicy(string name)
    {
        try
        {
            var policy = await _policyService.GetPolicyByNameAsync(name);
            
            if (policy == null)
            {
                return NotFound(new { result = new { status = false }, detail = new { message = $"Policy '{name}' not found" } });
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
                        name = policy.Name,
                        scope = policy.Scope,
                        active = policy.Active,
                        action = policy.Action,
                        realm = policy.Realm,
                        resolver = policy.Resolver,
                        user = policy.User,
                        client = policy.Client,
                        time = policy.Time,
                        admin_realm = policy.AdminRealm,
                        priority = policy.Priority,
                        check_all_resolvers = policy.CheckAllResolvers,
                        conditions = policy.Conditions.Select(c => new
                        {
                            section = c.Section,
                            key = c.Key,
                            comparator = c.Comparator,
                            value = c.Value,
                            active = c.Active
                        })
                    }
                },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting policy {Name}", name);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// POST /policy/{name} - Create or update policy
    /// </summary>
    [HttpPost("{name}")]
    public async Task<IActionResult> SetPolicy(string name, [FromBody] SetPolicyRequest request)
    {
        try
        {
            var parameters = new PolicyParameters
            {
                Name = name,
                Scope = request.Scope ?? "authorization",
                Active = request.Active ?? true,
                Action = request.Action,
                Realm = request.Realm,
                Resolver = request.Resolver,
                User = request.User,
                Client = request.Client,
                Time = request.Time,
                AdminRealm = request.AdminRealm,
                AdminUser = request.AdminUser,
                Priority = request.Priority ?? 1,
                CheckAllResolvers = request.CheckAllResolvers ?? false,
                Conditions = request.Conditions?.Select(c => new PolicyConditionParameters
                {
                    Section = c.Section,
                    Key = c.Key,
                    Comparator = c.Comparator,
                    Value = c.Value,
                    Active = c.Active ?? true
                }).ToList()
            };

            var policy = await _policyService.SetPolicyAsync(parameters);

            await _auditService.LogAsync("POLICY_SET", true, User.Identity?.Name, info: $"Set policy {name}");

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = new { name = policy.Name, id = policy.Id } },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting policy {Name}", name);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// DELETE /policy/{name} - Delete policy
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> DeletePolicy(string name)
    {
        try
        {
            var success = await _policyService.DeletePolicyAsync(name);

            if (!success)
            {
                return NotFound(new { result = new { status = false }, detail = new { message = $"Policy '{name}' not found" } });
            }

            await _auditService.LogAsync("POLICY_DELETE", true, User.Identity?.Name, info: $"Deleted policy {name}");

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
            _logger.LogError(ex, "Error deleting policy {Name}", name);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// POST /policy/enable/{name} - Enable policy
    /// </summary>
    [HttpPost("enable/{name}")]
    public async Task<IActionResult> EnablePolicy(string name)
    {
        try
        {
            var success = await _policyService.EnablePolicyAsync(name);

            if (!success)
            {
                return NotFound(new { result = new { status = false }, detail = new { message = $"Policy '{name}' not found" } });
            }

            await _auditService.LogAsync("POLICY_ENABLE", true, User.Identity?.Name, info: $"Enabled policy {name}");

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
            _logger.LogError(ex, "Error enabling policy {Name}", name);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// POST /policy/disable/{name} - Disable policy
    /// </summary>
    [HttpPost("disable/{name}")]
    public async Task<IActionResult> DisablePolicy(string name)
    {
        try
        {
            var success = await _policyService.DisablePolicyAsync(name);

            if (!success)
            {
                return NotFound(new { result = new { status = false }, detail = new { message = $"Policy '{name}' not found" } });
            }

            await _auditService.LogAsync("POLICY_DISABLE", true, User.Identity?.Name, info: $"Disabled policy {name}");

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
            _logger.LogError(ex, "Error disabling policy {Name}", name);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// GET /policy/defs - Get policy definitions
    /// </summary>
    [HttpGet("defs")]
    public IActionResult GetPolicyDefinitions([FromQuery] string? scope = null)
    {
        try
        {
            var scopes = _policyService.GetScopes();
            var definitions = new Dictionary<string, object>();

            foreach (var s in scopes)
            {
                if (scope != null && !s.Equals(scope, StringComparison.OrdinalIgnoreCase))
                    continue;

                var actions = _policyService.GetAvailableActions(s);
                definitions[s] = actions.Select(a => new
                {
                    name = a.Name,
                    description = a.Description,
                    type = a.Type,
                    @default = a.DefaultValue,
                    allowed_values = a.AllowedValues
                });
            }

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = definitions },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting policy definitions");
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }
}

#region Request Models

public class SetPolicyRequest
{
    public string? Scope { get; set; }
    public bool? Active { get; set; }
    public string? Action { get; set; }
    public string? Realm { get; set; }
    public string? Resolver { get; set; }
    public string? User { get; set; }
    public string? Client { get; set; }
    public string? Time { get; set; }
    public string? AdminRealm { get; set; }
    public string? AdminUser { get; set; }
    public int? Priority { get; set; }
    public bool? CheckAllResolvers { get; set; }
    public List<PolicyConditionRequest>? Conditions { get; set; }
}

public class PolicyConditionRequest
{
    public string Section { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Comparator { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool? Active { get; set; }
}

#endregion
