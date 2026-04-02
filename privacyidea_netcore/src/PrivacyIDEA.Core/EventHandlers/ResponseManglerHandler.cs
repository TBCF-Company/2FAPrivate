using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PrivacyIDEA.Core.EventHandlers;

/// <summary>
/// Event handler for modifying JSON response data
/// </summary>
public class ResponseManglerHandler : BaseEventHandler
{
    public ResponseManglerHandler(ILogger<ResponseManglerHandler> logger) : base(logger)
    {
    }

    public override string Identifier => "ResponseMangler";

    public override string Description => 
        "This event handler can mangle the JSON response.";

    public override IEnumerable<EventHandlerPosition> AllowedPositions => 
        new[] { EventHandlerPosition.Post };

    public override Dictionary<string, EventAction> Actions => new()
    {
        ["delete"] = new EventAction
        {
            Name = "delete",
            Options = new Dictionary<string, ActionOption>
            {
                ["JSON pointer"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The JSON pointer (key) that should be deleted. Please specify in the format '/detail/message'."
                }
            }
        },
        ["set"] = new EventAction
        {
            Name = "set",
            Options = new Dictionary<string, ActionOption>
            {
                ["JSON pointer"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The JSON pointer (key) that should be set. Please specify in the format '/detail/message'."
                },
                ["type"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The type of the value.",
                    Values = new List<string> { "string", "integer", "bool" }
                },
                ["value"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The value of the JSON key that should be set."
                }
            }
        }
    };

    public override Task<EventHandlerResult> ExecuteAsync(string action, EventHandlerOptions options)
    {
        var jsonPointer = GetStringOption(options, "JSON pointer");
        if (string.IsNullOrEmpty(jsonPointer))
        {
            return Task.FromResult(new EventHandlerResult
            {
                Success = false,
                Message = "No JSON pointer specified"
            });
        }

        // Parse the JSON pointer into components
        var components = ParseJsonPointer(jsonPointer);
        if (components.Length == 0)
        {
            return Task.FromResult(new EventHandlerResult
            {
                Success = false,
                Message = "Invalid JSON pointer format"
            });
        }

        if (components.Length > 3)
        {
            _logger.LogWarning("JSON pointer length of {Length} not supported", components.Length);
            return Task.FromResult(new EventHandlerResult
            {
                Success = false,
                Message = $"JSON pointer length of {components.Length} not supported (max 3)"
            });
        }

        var modifiedResponse = new Dictionary<string, object>(options.ResponseData);

        switch (action.ToLowerInvariant())
        {
            case "delete":
                return ExecuteDelete(components, modifiedResponse);

            case "set":
                return ExecuteSet(components, options, modifiedResponse);

            default:
                return Task.FromResult(new EventHandlerResult
                {
                    Success = false,
                    Message = $"Unknown action: {action}"
                });
        }
    }

    private Task<EventHandlerResult> ExecuteDelete(string[] components, Dictionary<string, object> modifiedResponse)
    {
        try
        {
            if (components.Length == 1)
            {
                if (modifiedResponse.ContainsKey(components[0]))
                {
                    modifiedResponse.Remove(components[0]);
                }
            }
            else if (components.Length == 2)
            {
                if (modifiedResponse.TryGetValue(components[0], out var level1) && 
                    level1 is Dictionary<string, object> level1Dict)
                {
                    level1Dict.Remove(components[1]);
                }
            }
            else if (components.Length == 3)
            {
                if (modifiedResponse.TryGetValue(components[0], out var level1) && 
                    level1 is Dictionary<string, object> level1Dict &&
                    level1Dict.TryGetValue(components[1], out var level2) &&
                    level2 is Dictionary<string, object> level2Dict)
                {
                    level2Dict.Remove(components[2]);
                }
            }

            _logger.LogInformation("Deleted JSON pointer: /{Pointer}", string.Join("/", components));

            return Task.FromResult(new EventHandlerResult
            {
                Success = true,
                Message = $"Deleted JSON pointer: /{string.Join("/", components)}",
                ModifiedResponseData = modifiedResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot delete response JSON pointer: /{Pointer}", string.Join("/", components));
            return Task.FromResult(new EventHandlerResult
            {
                Success = false,
                Message = $"Cannot delete JSON pointer: {ex.Message}"
            });
        }
    }

    private Task<EventHandlerResult> ExecuteSet(
        string[] components, 
        EventHandlerOptions options, 
        Dictionary<string, object> modifiedResponse)
    {
        var valueStr = GetStringOption(options, "value");
        var valueType = GetStringOption(options, "type") ?? "string";

        if (valueStr == null)
        {
            return Task.FromResult(new EventHandlerResult
            {
                Success = false,
                Message = "No value specified for set action"
            });
        }

        object value = ConvertValue(valueStr, valueType);

        try
        {
            if (components.Length == 1)
            {
                modifiedResponse[components[0]] = value;
            }
            else if (components.Length == 2)
            {
                if (!modifiedResponse.TryGetValue(components[0], out var level1) || 
                    level1 is not Dictionary<string, object> level1Dict)
                {
                    level1Dict = new Dictionary<string, object>();
                    modifiedResponse[components[0]] = level1Dict;
                }
                else
                {
                    level1Dict = (Dictionary<string, object>)level1;
                }
                level1Dict[components[1]] = value;
            }
            else if (components.Length == 3)
            {
                if (!modifiedResponse.TryGetValue(components[0], out var level1) || 
                    level1 is not Dictionary<string, object> level1Dict)
                {
                    level1Dict = new Dictionary<string, object>();
                    modifiedResponse[components[0]] = level1Dict;
                }
                else
                {
                    level1Dict = (Dictionary<string, object>)level1;
                }

                if (!level1Dict.TryGetValue(components[1], out var level2) || 
                    level2 is not Dictionary<string, object> level2Dict)
                {
                    level2Dict = new Dictionary<string, object>();
                    level1Dict[components[1]] = level2Dict;
                }
                else
                {
                    level2Dict = (Dictionary<string, object>)level2;
                }
                level2Dict[components[2]] = value;
            }

            _logger.LogInformation("Set JSON pointer /{Pointer} to {Value}", string.Join("/", components), value);

            return Task.FromResult(new EventHandlerResult
            {
                Success = true,
                Message = $"Set JSON pointer: /{string.Join("/", components)}",
                ModifiedResponseData = modifiedResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot set response JSON pointer: /{Pointer}", string.Join("/", components));
            return Task.FromResult(new EventHandlerResult
            {
                Success = false,
                Message = $"Cannot set JSON pointer: {ex.Message}"
            });
        }
    }

    private static string[] ParseJsonPointer(string pointer)
    {
        var components = pointer.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return components;
    }

    private static object ConvertValue(string valueStr, string type)
    {
        return type.ToLowerInvariant() switch
        {
            "integer" => int.TryParse(valueStr, out var intVal) ? intVal : 0,
            "bool" => bool.TryParse(valueStr, out var boolVal) ? boolVal : 
                      valueStr.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                      valueStr.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                      valueStr.Equals("yes", StringComparison.OrdinalIgnoreCase),
            _ => valueStr
        };
    }
}
