using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PrivacyIDEA.Core.EventHandlers;

/// <summary>
/// Event handler position in the request pipeline
/// </summary>
public enum EventHandlerPosition
{
    Pre,
    Post
}

/// <summary>
/// Event handler action option definition
/// </summary>
public class ActionOption
{
    public string Type { get; set; } = "str";
    public bool Required { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string>? Values { get; set; }
}

/// <summary>
/// Event handler action definition
/// </summary>
public class EventAction
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, ActionOption> Options { get; set; } = new();
}

/// <summary>
/// Event handler options passed during execution
/// </summary>
public class EventHandlerOptions
{
    public string? ClientIp { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? Realm { get; set; }
    public string? TokenSerial { get; set; }
    public string? TokenType { get; set; }
    public Dictionary<string, object> RequestData { get; set; } = new();
    public Dictionary<string, object> ResponseData { get; set; } = new();
    public Dictionary<string, object> HandlerDefinition { get; set; } = new();
    public Dictionary<string, object> HandlerOptions { get; set; } = new();
}

/// <summary>
/// Event handler result
/// </summary>
public class EventHandlerResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object>? ModifiedRequestData { get; set; }
    public Dictionary<string, object>? ModifiedResponseData { get; set; }
}

/// <summary>
/// Base class for all event handlers
/// </summary>
public abstract class BaseEventHandler
{
    protected readonly ILogger _logger;

    protected BaseEventHandler(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Unique identifier for this event handler type
    /// </summary>
    public abstract string Identifier { get; }

    /// <summary>
    /// Description of what this event handler does
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Allowed positions in the request pipeline (Pre, Post, or both)
    /// </summary>
    public virtual IEnumerable<EventHandlerPosition> AllowedPositions => 
        new[] { EventHandlerPosition.Post };

    /// <summary>
    /// Returns the available actions and their options
    /// </summary>
    public abstract Dictionary<string, EventAction> Actions { get; }

    /// <summary>
    /// Execute the event handler action
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <param name="options">Event handler options</param>
    /// <returns>Result of the event handler execution</returns>
    public abstract Task<EventHandlerResult> ExecuteAsync(string action, EventHandlerOptions options);

    /// <summary>
    /// Check if a condition is met for the event handler to execute
    /// </summary>
    protected virtual bool CheckCondition(EventHandlerOptions options, string conditionName, object conditionValue)
    {
        return true; // Default: all conditions pass
    }

    /// <summary>
    /// Get a string option from handler options
    /// </summary>
    protected string? GetStringOption(EventHandlerOptions options, string key)
    {
        if (options.HandlerOptions.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }
        return null;
    }

    /// <summary>
    /// Get a boolean option from handler options
    /// </summary>
    protected bool GetBoolOption(EventHandlerOptions options, string key, bool defaultValue = false)
    {
        if (options.HandlerOptions.TryGetValue(key, out var value))
        {
            if (value is bool boolValue) return boolValue;
            if (value is string strValue) return bool.TryParse(strValue, out var result) && result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Get an integer option from handler options
    /// </summary>
    protected int GetIntOption(EventHandlerOptions options, string key, int defaultValue = 0)
    {
        if (options.HandlerOptions.TryGetValue(key, out var value))
        {
            if (value is int intValue) return intValue;
            if (value is string strValue && int.TryParse(strValue, out var result)) return result;
        }
        return defaultValue;
    }
}
