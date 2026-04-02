using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;
using System.Text.RegularExpressions;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// Policy Service implementation
/// Maps to Python: privacyidea/lib/policy.py
/// </summary>
public class PolicyService : IPolicyService
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PolicyService> _logger;

    // Policy scopes
    private static readonly string[] Scopes = { "admin", "user", "authorization", "authentication", "enrollment", "webui" };

    // Policy actions by scope
    private static readonly Dictionary<string, List<PolicyActionDefinition>> ActionDefinitions = new()
    {
        ["admin"] = new()
        {
            new() { Name = "tokenlist", Description = "Allow admin to list tokens", Type = "bool" },
            new() { Name = "tokenassign", Description = "Allow admin to assign tokens", Type = "bool" },
            new() { Name = "tokendelete", Description = "Allow admin to delete tokens", Type = "bool" },
            new() { Name = "tokenenable", Description = "Allow admin to enable tokens", Type = "bool" },
            new() { Name = "tokendisable", Description = "Allow admin to disable tokens", Type = "bool" },
            new() { Name = "tokenrealm", Description = "Allow admin to manage token realms", Type = "bool" },
            new() { Name = "userlist", Description = "Allow admin to list users", Type = "bool" },
        },
        ["user"] = new()
        {
            new() { Name = "enrollTOTP", Description = "Allow user to enroll TOTP token", Type = "bool" },
            new() { Name = "enrollHOTP", Description = "Allow user to enroll HOTP token", Type = "bool" },
            new() { Name = "enrollPUSH", Description = "Allow user to enroll Push token", Type = "bool" },
            new() { Name = "enrollWEBAUTHN", Description = "Allow user to enroll WebAuthn token", Type = "bool" },
            new() { Name = "delete", Description = "Allow user to delete own tokens", Type = "bool" },
            new() { Name = "disable", Description = "Allow user to disable own tokens", Type = "bool" },
            new() { Name = "setpin", Description = "Allow user to set PIN", Type = "bool" },
        },
        ["authorization"] = new()
        {
            new() { Name = "tokentype", Description = "Required token types", Type = "string" },
            new() { Name = "serial", Description = "Required serial pattern", Type = "string" },
            new() { Name = "setrealm", Description = "Override realm", Type = "string" },
            new() { Name = "no_detail_on_fail", Description = "Hide details on failure", Type = "bool" },
            new() { Name = "no_detail_on_success", Description = "Hide details on success", Type = "bool" },
        },
        ["authentication"] = new()
        {
            new() { Name = "otppin", Description = "PIN policy", Type = "string", AllowedValues = new[] { "tokenpin", "userstore", "none" } },
            new() { Name = "passthru", Description = "Pass through authentication", Type = "bool" },
            new() { Name = "passOnNoToken", Description = "Allow login without token", Type = "bool" },
            new() { Name = "passOnNoUser", Description = "Allow login for unknown users", Type = "bool" },
            new() { Name = "mangle", Description = "Mangle user input", Type = "string" },
        },
        ["enrollment"] = new()
        {
            new() { Name = "tokenlabel", Description = "Token label pattern", Type = "string" },
            new() { Name = "otp_pin_random", Description = "Generate random PIN length", Type = "int" },
            new() { Name = "otp_pin_contents", Description = "PIN character requirements", Type = "string" },
            new() { Name = "autoassignment", Description = "Auto assign tokens", Type = "bool" },
        },
        ["webui"] = new()
        {
            new() { Name = "login_mode", Description = "Login mode", Type = "string" },
            new() { Name = "logout_time", Description = "Session timeout in seconds", Type = "int" },
            new() { Name = "token_page_size", Description = "Tokens per page", Type = "int" },
        }
    };

    public PolicyService(
        IUnitOfWork unitOfWork,
        ILogger<PolicyService> logger)
    {
        _unitOfWork = unitOfWork;
        _policyRepository = unitOfWork.Policies;
        _logger = logger;
    }

    public async Task<IEnumerable<Policy>> GetPoliciesAsync(string? scope = null, bool? active = null)
    {
        if (!string.IsNullOrEmpty(scope))
        {
            return await _policyRepository.GetByScopeAsync(scope.ToLower(), active);
        }
        return await _policyRepository.GetAllAsync(active);
    }

    public async Task<Policy?> GetPolicyByNameAsync(string name)
    {
        return await _policyRepository.GetByNameAsync(name);
    }

    public async Task<Policy> SetPolicyAsync(PolicyParameters parameters)
    {
        var existing = await _policyRepository.GetByNameAsync(parameters.Name);

        if (existing != null)
        {
            // Update existing policy
            existing.Scope = parameters.Scope.ToLower();
            existing.Active = parameters.Active;
            existing.Action = parameters.Action;
            existing.Realm = parameters.Realm;
            existing.Resolver = parameters.Resolver;
            existing.User = parameters.User;
            existing.Client = parameters.Client;
            existing.Time = parameters.Time;
            existing.AdminRealm = parameters.AdminRealm;
            existing.Priority = parameters.Priority;
            existing.CheckAllResolvers = parameters.CheckAllResolvers;

            await _policyRepository.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated policy {PolicyName}", parameters.Name);
            return existing;
        }

        // Create new policy
        var policy = new Policy
        {
            Name = parameters.Name,
            Scope = parameters.Scope.ToLower(),
            Active = parameters.Active,
            Action = parameters.Action,
            Realm = parameters.Realm,
            Resolver = parameters.Resolver,
            User = parameters.User,
            Client = parameters.Client,
            Time = parameters.Time,
            AdminRealm = parameters.AdminRealm,
            Priority = parameters.Priority,
            CheckAllResolvers = parameters.CheckAllResolvers
        };

        await _policyRepository.AddAsync(policy);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created policy {PolicyName} with scope {Scope}", policy.Name, policy.Scope);
        return policy;
    }

    public async Task<bool> DeletePolicyAsync(string name)
    {
        var policy = await _policyRepository.GetByNameAsync(name);
        if (policy == null) return false;

        await _policyRepository.DeleteAsync(policy);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted policy {PolicyName}", name);
        return true;
    }

    public async Task<bool> EnablePolicyAsync(string name)
    {
        var policy = await _policyRepository.GetByNameAsync(name);
        if (policy == null) return false;

        policy.Active = true;
        await _policyRepository.UpdateAsync(policy);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DisablePolicyAsync(string name)
    {
        var policy = await _policyRepository.GetByNameAsync(name);
        if (policy == null) return false;

        policy.Active = false;
        await _policyRepository.UpdateAsync(policy);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Policy>> GetMatchingPoliciesAsync(PolicyMatchRequest request)
    {
        var allPolicies = await _policyRepository.GetAllAsync(true);
        var matchingPolicies = new List<Policy>();

        foreach (var policy in allPolicies.OrderBy(p => p.Priority))
        {
            if (MatchesPolicy(policy, request))
            {
                matchingPolicies.Add(policy);
            }
        }

        return matchingPolicies;
    }

    public async Task<string?> GetActionValueAsync(string scope, string action, PolicyMatchRequest? request = null)
    {
        var policies = await GetPoliciesAsync(scope, true);
        var matchingPolicies = request != null
            ? policies.Where(p => MatchesPolicy(p, request))
            : policies;

        // Higher priority policies override lower ones
        foreach (var policy in matchingPolicies.OrderByDescending(p => p.Priority))
        {
            var actions = ParseActions(policy.Action ?? "");
            if (actions.TryGetValue(action, out var value))
            {
                return value;
            }
        }

        return null;
    }

    public async Task<bool> IsActionAllowedAsync(string scope, string action, PolicyMatchRequest? request = null)
    {
        var value = await GetActionValueAsync(scope, action, request);
        if (string.IsNullOrEmpty(value)) return false;
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    public IEnumerable<PolicyActionDefinition> GetAvailableActions(string scope)
    {
        if (ActionDefinitions.TryGetValue(scope.ToLower(), out var actions))
        {
            return actions;
        }
        return Enumerable.Empty<PolicyActionDefinition>();
    }

    public IEnumerable<string> GetScopes()
    {
        return Scopes;
    }

    private bool MatchesPolicy(Policy policy, PolicyMatchRequest request)
    {
        // Check realm
        if (!string.IsNullOrEmpty(policy.Realm))
        {
            var realms = policy.Realm.Split(',').Select(r => r.Trim());
            if (!string.IsNullOrEmpty(request.Realm) && !realms.Any(r => MatchesWildcard(request.Realm, r)))
                return false;
        }

        // Check resolver
        if (!string.IsNullOrEmpty(policy.Resolver))
        {
            var resolvers = policy.Resolver.Split(',').Select(r => r.Trim());
            if (!string.IsNullOrEmpty(request.Resolver) && !resolvers.Any(r => MatchesWildcard(request.Resolver, r)))
                return false;
        }

        // Check user
        if (!string.IsNullOrEmpty(policy.User))
        {
            var users = policy.User.Split(',').Select(u => u.Trim());
            if (!string.IsNullOrEmpty(request.User) && !users.Any(u => MatchesWildcard(request.User, u)))
                return false;
        }

        // Check client IP
        if (!string.IsNullOrEmpty(policy.Client) && !string.IsNullOrEmpty(request.Client))
        {
            if (!MatchesClientIp(request.Client, policy.Client))
                return false;
        }

        // Check time constraints
        if (!string.IsNullOrEmpty(policy.Time))
        {
            var time = request.Time ?? DateTime.Now;
            if (!MatchesTime(time, policy.Time))
                return false;
        }

        // Check admin realm for admin policies
        if (policy.Scope == "admin" && !string.IsNullOrEmpty(policy.AdminRealm))
        {
            var adminRealms = policy.AdminRealm.Split(',').Select(r => r.Trim());
            if (!string.IsNullOrEmpty(request.AdminRealm) && !adminRealms.Contains(request.AdminRealm))
                return false;
        }

        return true;
    }

    private static bool MatchesWildcard(string value, string pattern)
    {
        if (pattern == "*") return true;
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
    }

    private static bool MatchesClientIp(string clientIp, string policyClient)
    {
        var patterns = policyClient.Split(',').Select(p => p.Trim());

        foreach (var pattern in patterns)
        {
            var isExclude = pattern.StartsWith("!");
            var actualPattern = isExclude ? pattern[1..] : pattern;

            bool matches;
            if (actualPattern.Contains('/'))
            {
                matches = IsIpInCidr(clientIp, actualPattern);
            }
            else if (actualPattern.Contains('*'))
            {
                matches = MatchesWildcard(clientIp, actualPattern);
            }
            else
            {
                matches = clientIp == actualPattern;
            }

            if (isExclude && matches) return false;
            if (!isExclude && matches) return true;
        }

        return false;
    }

    private static bool IsIpInCidr(string ip, string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            var networkIp = System.Net.IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);
            var clientIp = System.Net.IPAddress.Parse(ip);

            var networkBytes = networkIp.GetAddressBytes();
            var clientBytes = clientIp.GetAddressBytes();

            if (networkBytes.Length != clientBytes.Length) return false;

            int fullBytes = prefixLength / 8;
            int remainingBits = prefixLength % 8;

            for (int i = 0; i < fullBytes; i++)
            {
                if (networkBytes[i] != clientBytes[i]) return false;
            }

            if (remainingBits > 0 && fullBytes < networkBytes.Length)
            {
                int mask = 0xFF << (8 - remainingBits);
                if ((networkBytes[fullBytes] & mask) != (clientBytes[fullBytes] & mask)) return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool MatchesTime(DateTime now, string timeSpec)
    {
        try
        {
            var parts = timeSpec.Split(':');
            if (parts.Length < 2) return true;

            var dayPart = parts[0];
            var timePart = parts[1];

            var currentDay = now.DayOfWeek.ToString()[..3];
            if (dayPart.Contains('-'))
            {
                var days = dayPart.Split('-');
                var startDay = ParseDayOfWeek(days[0]);
                var endDay = ParseDayOfWeek(days[1]);
                var nowDay = (int)now.DayOfWeek;
                if (nowDay < startDay || nowDay > endDay) return false;
            }
            else if (!dayPart.Equals(currentDay, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var times = timePart.Split('-');
            if (times.Length >= 2)
            {
                var startTime = TimeOnly.ParseExact(times[0], "HHmm");
                var endTime = TimeOnly.ParseExact(times[1], "HHmm");
                var nowTime = TimeOnly.FromDateTime(now);

                return nowTime >= startTime && nowTime <= endTime;
            }

            return true;
        }
        catch
        {
            return true;
        }
    }

    private static int ParseDayOfWeek(string day)
    {
        return day.ToLower() switch
        {
            "sun" => 0, "mon" => 1, "tue" => 2, "wed" => 3,
            "thu" => 4, "fri" => 5, "sat" => 6, _ => 0
        };
    }

    private static Dictionary<string, string> ParseActions(string actionString)
    {
        var actions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(actionString)) return actions;

        foreach (var pair in actionString.Split(','))
        {
            var parts = pair.Split('=', 2);
            var key = parts[0].Trim();
            var value = parts.Length > 1 ? parts[1].Trim() : "true";
            actions[key] = value;
        }

        return actions;
    }
}
