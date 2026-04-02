using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Interface for Policy Service
/// Maps to Python: privacyidea/lib/policy.py
/// </summary>
public interface IPolicyService
{
    /// <summary>
    /// Get all policies
    /// </summary>
    Task<IEnumerable<Policy>> GetPoliciesAsync(string? scope = null, bool? active = null, string? realm = null, string? user = null);

    /// <summary>
    /// Get policy by name
    /// </summary>
    Task<Policy?> GetPolicyByNameAsync(string name);

    /// <summary>
    /// Create or update a policy
    /// </summary>
    Task<Policy> SetPolicyAsync(PolicyParameters parameters);

    /// <summary>
    /// Delete a policy
    /// </summary>
    Task<bool> DeletePolicyAsync(string name);

    /// <summary>
    /// Enable a policy
    /// </summary>
    Task<bool> EnablePolicyAsync(string name);

    /// <summary>
    /// Disable a policy
    /// </summary>
    Task<bool> DisablePolicyAsync(string name);

    /// <summary>
    /// Get matching policies for a request
    /// </summary>
    Task<IEnumerable<Policy>> GetMatchingPoliciesAsync(PolicyMatchRequest request);

    /// <summary>
    /// Get action value from matching policies
    /// </summary>
    Task<string?> GetActionValueAsync(string scope, string action, PolicyMatchRequest? request = null);

    /// <summary>
    /// Check if action is allowed
    /// </summary>
    Task<bool> IsActionAllowedAsync(string scope, string action, PolicyMatchRequest? request = null);

    /// <summary>
    /// Get all available policy actions for a scope
    /// </summary>
    IEnumerable<PolicyActionDefinition> GetAvailableActions(string scope);

    /// <summary>
    /// Get all policy scopes
    /// </summary>
    IEnumerable<string> GetScopes();
}

/// <summary>
/// Parameters for creating/updating a policy
/// </summary>
public class PolicyParameters
{
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public string? Action { get; set; }
    public string? Realm { get; set; }
    public string? Resolver { get; set; }
    public string? User { get; set; }
    public string? Client { get; set; }
    public string? Time { get; set; }
    public string? AdminRealm { get; set; }
    public string? AdminUser { get; set; }
    public int Priority { get; set; } = 1;
    public bool CheckAllResolvers { get; set; } = false;
    public List<PolicyConditionParameters>? Conditions { get; set; }
}

/// <summary>
/// Policy condition parameters
/// </summary>
public class PolicyConditionParameters
{
    public string Section { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Comparator { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool Active { get; set; } = true;
}

/// <summary>
/// Request context for policy matching
/// </summary>
public class PolicyMatchRequest
{
    public string? User { get; set; }
    public string? Realm { get; set; }
    public string? Resolver { get; set; }
    public string? Client { get; set; }
    public string? AdminUser { get; set; }
    public string? AdminRealm { get; set; }
    public string? Serial { get; set; }
    public string? TokenType { get; set; }
    public DateTime? Time { get; set; }
    public Dictionary<string, string>? CustomAttributes { get; set; }
}

/// <summary>
/// Policy action definition
/// </summary>
public class PolicyActionDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "bool"; // bool, string, int
    public string? DefaultValue { get; set; }
    public IEnumerable<string>? AllowedValues { get; set; }
}
