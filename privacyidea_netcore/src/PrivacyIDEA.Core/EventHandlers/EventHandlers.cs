using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.EventHandlers;

/// <summary>
/// Interface for event handler implementations
/// Maps to Python: privacyidea/lib/eventhandler/base.py
/// </summary>
public interface IEventHandler
{
    /// <summary>
    /// Handler type name
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Display name for UI
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Available actions for this handler type
    /// </summary>
    IEnumerable<string> AvailableActions { get; }

    /// <summary>
    /// Available conditions for this handler type
    /// </summary>
    IEnumerable<string> AvailableConditions { get; }

    /// <summary>
    /// Initialize the handler with configuration
    /// </summary>
    void Initialize(Dictionary<string, string> options);

    /// <summary>
    /// Execute the handler action
    /// </summary>
    Task<EventHandlerResult> ExecuteAsync(EventContext context, string action, Dictionary<string, string> options);

    /// <summary>
    /// Check if conditions are met
    /// </summary>
    Task<bool> CheckConditionsAsync(EventContext context, Dictionary<string, string> conditions);
}

/// <summary>
/// Context for event handler execution
/// </summary>
public class EventContext
{
    public string EventName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? Realm { get; set; }
    public string? Serial { get; set; }
    public string? TokenType { get; set; }
    public string? ClientIp { get; set; }
    public bool? AuthSuccess { get; set; }
    public string? TransactionId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of event handler execution
/// </summary>
public class EventHandlerResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object>? ResultData { get; set; }
}

/// <summary>
/// Base class for event handlers
/// </summary>
public abstract class EventHandlerBase : IEventHandler
{
    protected readonly ILogger Logger;

    protected EventHandlerBase(ILogger logger)
    {
        Logger = logger;
    }

    public abstract string Type { get; }
    public abstract string DisplayName { get; }
    public abstract IEnumerable<string> AvailableActions { get; }
    public virtual IEnumerable<string> AvailableConditions => new[]
    {
        "user", "realm", "tokentype", "serial", "client_ip", "result_status"
    };

    public virtual void Initialize(Dictionary<string, string> options)
    {
    }

    public abstract Task<EventHandlerResult> ExecuteAsync(EventContext context, string action, Dictionary<string, string> options);

    public virtual Task<bool> CheckConditionsAsync(EventContext context, Dictionary<string, string> conditions)
    {
        foreach (var (key, value) in conditions)
        {
            var contextValue = key.ToLower() switch
            {
                "user" => context.Username ?? context.UserId,
                "realm" => context.Realm,
                "tokentype" => context.TokenType,
                "serial" => context.Serial,
                "client_ip" => context.ClientIp,
                "result_status" => context.AuthSuccess?.ToString().ToLower(),
                _ => context.Data.GetValueOrDefault(key)?.ToString()
            };

            if (!MatchCondition(contextValue, value))
                return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    protected static bool MatchCondition(string? actualValue, string expectedValue)
    {
        if (string.IsNullOrEmpty(actualValue))
            return string.IsNullOrEmpty(expectedValue);

        // Support wildcards
        if (expectedValue.Contains('*'))
        {
            var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(expectedValue).Replace("\\*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(actualValue, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// User Notification Event Handler
/// Maps to Python: privacyidea/lib/eventhandler/usernotification.py
/// </summary>
public class UserNotificationHandler : EventHandlerBase
{
    private readonly IEmailService? _emailService;
    private readonly ISmsService? _smsService;

    public override string Type => "usernotification";
    public override string DisplayName => "User Notification Handler";
    public override IEnumerable<string> AvailableActions => new[]
    {
        "sendmail", "sendsms"
    };

    public UserNotificationHandler(ILogger<UserNotificationHandler> logger, IEmailService? emailService = null, ISmsService? smsService = null) 
        : base(logger)
    {
        _emailService = emailService;
        _smsService = smsService;
    }

    public override async Task<EventHandlerResult> ExecuteAsync(EventContext context, string action, Dictionary<string, string> options)
    {
        Logger.LogInformation("Executing {Action} for event {Event}", action, context.EventName);

        return action.ToLower() switch
        {
            "sendmail" => await SendEmailAsync(context, options),
            "sendsms" => await SendSmsAsync(context, options),
            _ => new EventHandlerResult { Success = false, Message = $"Unknown action: {action}" }
        };
    }

    private async Task<EventHandlerResult> SendEmailAsync(EventContext context, Dictionary<string, string> options)
    {
        if (_emailService == null)
            return new EventHandlerResult { Success = false, Message = "Email service not configured" };

        var to = options.GetValueOrDefault("to", "{user_email}");
        var subject = options.GetValueOrDefault("subject", "PrivacyIDEA Notification");
        var body = options.GetValueOrDefault("body", "An event occurred: {event_name}");

        // Replace placeholders
        to = ReplacePlaceholders(to, context);
        subject = ReplacePlaceholders(subject, context);
        body = ReplacePlaceholders(body, context);

        try
        {
            await _emailService.SendAsync(to, subject, body);
            return new EventHandlerResult { Success = true, Message = $"Email sent to {to}" };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send email");
            return new EventHandlerResult { Success = false, Message = ex.Message };
        }
    }

    private async Task<EventHandlerResult> SendSmsAsync(EventContext context, Dictionary<string, string> options)
    {
        if (_smsService == null)
            return new EventHandlerResult { Success = false, Message = "SMS service not configured" };

        var to = options.GetValueOrDefault("to", "{user_mobile}");
        var message = options.GetValueOrDefault("message", "PrivacyIDEA: {event_name}");

        to = ReplacePlaceholders(to, context);
        message = ReplacePlaceholders(message, context);

        try
        {
            await _smsService.SendAsync(to, message);
            return new EventHandlerResult { Success = true, Message = $"SMS sent to {to}" };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send SMS");
            return new EventHandlerResult { Success = false, Message = ex.Message };
        }
    }

    private static string ReplacePlaceholders(string template, EventContext context)
    {
        return template
            .Replace("{user}", context.Username ?? context.UserId ?? "")
            .Replace("{realm}", context.Realm ?? "")
            .Replace("{serial}", context.Serial ?? "")
            .Replace("{tokentype}", context.TokenType ?? "")
            .Replace("{client_ip}", context.ClientIp ?? "")
            .Replace("{event_name}", context.EventName)
            .Replace("{timestamp}", context.Timestamp.ToString("u"));
    }
}

/// <summary>
/// Token Event Handler
/// Maps to Python: privacyidea/lib/eventhandler/tokenhandler.py
/// </summary>
public class TokenEventHandler : EventHandlerBase
{
    private readonly ITokenService _tokenService;

    public override string Type => "token";
    public override string DisplayName => "Token Handler";
    public override IEnumerable<string> AvailableActions => new[]
    {
        "enable", "disable", "delete", "unassign", "set", "setpin", 
        "settokeninfo", "resync", "reset_failcount", "setdescription"
    };

    public TokenEventHandler(ILogger<TokenEventHandler> logger, ITokenService tokenService) : base(logger)
    {
        _tokenService = tokenService;
    }

    public override async Task<EventHandlerResult> ExecuteAsync(EventContext context, string action, Dictionary<string, string> options)
    {
        Logger.LogInformation("Executing token action {Action} for event {Event}, serial {Serial}", 
            action, context.EventName, context.Serial);

        if (string.IsNullOrEmpty(context.Serial))
            return new EventHandlerResult { Success = false, Message = "No serial in context" };

        try
        {
            var result = action.ToLower() switch
            {
                "enable" => await _tokenService.EnableTokenAsync(context.Serial),
                "disable" => await _tokenService.DisableTokenAsync(context.Serial),
                "delete" => await _tokenService.DeleteTokenAsync(context.Serial),
                "unassign" => await _tokenService.UnassignTokenAsync(context.Serial),
                "reset_failcount" => await _tokenService.ResetFailCounterAsync(context.Serial),
                "setpin" when options.TryGetValue("pin", out var pin) => await _tokenService.SetPinAsync(context.Serial, pin),
                "settokeninfo" when options.TryGetValue("key", out var key) && options.TryGetValue("value", out var value) 
                    => await _tokenService.SetTokenInfoAsync(context.Serial, key, value),
                _ => false
            };

            return new EventHandlerResult 
            { 
                Success = result, 
                Message = result ? $"Action {action} completed" : $"Action {action} failed" 
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Token handler action failed");
            return new EventHandlerResult { Success = false, Message = ex.Message };
        }
    }
}

/// <summary>
/// Webhook Event Handler
/// Maps to Python: privacyidea/lib/eventhandler/webhookhandler.py
/// </summary>
public class WebhookEventHandler : EventHandlerBase
{
    private readonly HttpClient _httpClient;

    public override string Type => "webhook";
    public override string DisplayName => "Webhook Handler";
    public override IEnumerable<string> AvailableActions => new[]
    {
        "post", "get", "put", "delete"
    };

    public WebhookEventHandler(ILogger<WebhookEventHandler> logger, HttpClient? httpClient = null) : base(logger)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public override async Task<EventHandlerResult> ExecuteAsync(EventContext context, string action, Dictionary<string, string> options)
    {
        var url = options.GetValueOrDefault("url", "");
        if (string.IsNullOrEmpty(url))
            return new EventHandlerResult { Success = false, Message = "No URL specified" };

        Logger.LogInformation("Executing webhook {Action} to {Url}", action, url);

        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                @event = context.EventName,
                user = context.Username ?? context.UserId,
                realm = context.Realm,
                serial = context.Serial,
                tokentype = context.TokenType,
                client_ip = context.ClientIp,
                auth_success = context.AuthSuccess,
                timestamp = context.Timestamp,
                data = context.Data
            });

            HttpResponseMessage response;
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

            response = action.ToLower() switch
            {
                "post" => await _httpClient.PostAsync(url, content),
                "put" => await _httpClient.PutAsync(url, content),
                "delete" => await _httpClient.DeleteAsync(url),
                _ => await _httpClient.GetAsync(url)
            };

            return new EventHandlerResult
            {
                Success = response.IsSuccessStatusCode,
                Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                ResultData = new Dictionary<string, object>
                {
                    ["status_code"] = (int)response.StatusCode
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Webhook request failed");
            return new EventHandlerResult { Success = false, Message = ex.Message };
        }
    }
}

/// <summary>
/// Counter Event Handler
/// Maps to Python: privacyidea/lib/eventhandler/counterhandler.py
/// Increments/decrements counters
/// </summary>
public class CounterEventHandler : EventHandlerBase
{
    private static readonly Dictionary<string, long> _counters = new();
    private static readonly object _lock = new();

    public override string Type => "counter";
    public override string DisplayName => "Counter Handler";
    public override IEnumerable<string> AvailableActions => new[]
    {
        "increase", "decrease", "reset"
    };

    public CounterEventHandler(ILogger<CounterEventHandler> logger) : base(logger)
    {
    }

    public override Task<EventHandlerResult> ExecuteAsync(EventContext context, string action, Dictionary<string, string> options)
    {
        var counterName = options.GetValueOrDefault("counter_name", "default");
        var amount = int.TryParse(options.GetValueOrDefault("amount", "1"), out var a) ? a : 1;

        Logger.LogInformation("Executing counter {Action} on {Counter}", action, counterName);

        lock (_lock)
        {
            if (!_counters.ContainsKey(counterName))
                _counters[counterName] = 0;

            switch (action.ToLower())
            {
                case "increase":
                    _counters[counterName] += amount;
                    break;
                case "decrease":
                    _counters[counterName] -= amount;
                    break;
                case "reset":
                    _counters[counterName] = 0;
                    break;
            }

            return Task.FromResult(new EventHandlerResult
            {
                Success = true,
                Message = $"Counter {counterName} = {_counters[counterName]}",
                ResultData = new Dictionary<string, object>
                {
                    ["counter_value"] = _counters[counterName]
                }
            });
        }
    }

    public static long GetCounterValue(string counterName)
    {
        lock (_lock)
        {
            return _counters.GetValueOrDefault(counterName, 0);
        }
    }
}

/// <summary>
/// Script Event Handler
/// Maps to Python: privacyidea/lib/eventhandler/scripthandler.py
/// Executes external scripts
/// </summary>
public class ScriptEventHandler : EventHandlerBase
{
    public override string Type => "script";
    public override string DisplayName => "Script Handler";
    public override IEnumerable<string> AvailableActions => new[]
    {
        "run"
    };

    public ScriptEventHandler(ILogger<ScriptEventHandler> logger) : base(logger)
    {
    }

    public override async Task<EventHandlerResult> ExecuteAsync(EventContext context, string action, Dictionary<string, string> options)
    {
        var scriptPath = options.GetValueOrDefault("script", "");
        if (string.IsNullOrEmpty(scriptPath))
            return new EventHandlerResult { Success = false, Message = "No script specified" };

        Logger.LogInformation("Executing script {Script}", scriptPath);

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Add environment variables
            startInfo.EnvironmentVariables["PI_EVENT"] = context.EventName;
            startInfo.EnvironmentVariables["PI_USER"] = context.Username ?? context.UserId ?? "";
            startInfo.EnvironmentVariables["PI_REALM"] = context.Realm ?? "";
            startInfo.EnvironmentVariables["PI_SERIAL"] = context.Serial ?? "";
            startInfo.EnvironmentVariables["PI_TOKENTYPE"] = context.TokenType ?? "";
            startInfo.EnvironmentVariables["PI_CLIENT_IP"] = context.ClientIp ?? "";

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
                return new EventHandlerResult { Success = false, Message = "Failed to start script" };

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return new EventHandlerResult
            {
                Success = process.ExitCode == 0,
                Message = string.IsNullOrEmpty(error) ? output : error,
                ResultData = new Dictionary<string, object>
                {
                    ["exit_code"] = process.ExitCode,
                    ["stdout"] = output,
                    ["stderr"] = error
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Script execution failed");
            return new EventHandlerResult { Success = false, Message = ex.Message };
        }
    }
}

/// <summary>
/// Logging Event Handler
/// Maps to Python: privacyidea/lib/eventhandler/logginghandler.py
/// </summary>
public class LoggingEventHandler : EventHandlerBase
{
    public override string Type => "logging";
    public override string DisplayName => "Logging Handler";
    public override IEnumerable<string> AvailableActions => new[]
    {
        "info", "warning", "error", "debug"
    };

    public LoggingEventHandler(ILogger<LoggingEventHandler> logger) : base(logger)
    {
    }

    public override Task<EventHandlerResult> ExecuteAsync(EventContext context, string action, Dictionary<string, string> options)
    {
        var message = options.GetValueOrDefault("message", "{event_name}: user={user}, serial={serial}")
            .Replace("{event_name}", context.EventName)
            .Replace("{user}", context.Username ?? context.UserId ?? "")
            .Replace("{realm}", context.Realm ?? "")
            .Replace("{serial}", context.Serial ?? "")
            .Replace("{tokentype}", context.TokenType ?? "")
            .Replace("{client_ip}", context.ClientIp ?? "")
            .Replace("{timestamp}", context.Timestamp.ToString("u"));

        switch (action.ToLower())
        {
            case "debug":
                Logger.LogDebug("{Message}", message);
                break;
            case "info":
                Logger.LogInformation("{Message}", message);
                break;
            case "warning":
                Logger.LogWarning("{Message}", message);
                break;
            case "error":
                Logger.LogError("{Message}", message);
                break;
        }

        return Task.FromResult(new EventHandlerResult { Success = true, Message = "Logged" });
    }
}

/// <summary>
/// Email service interface (for dependency injection)
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

/// <summary>
/// SMS service interface (for dependency injection)
/// </summary>
public interface ISmsService
{
    Task SendAsync(string to, string message);
}
