using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PrivacyIDEA.Core.EventHandlers;

/// <summary>
/// Event handler for modifying request parameters
/// </summary>
public class RequestManglerHandler : BaseEventHandler
{
    public RequestManglerHandler(ILogger<RequestManglerHandler> logger) : base(logger)
    {
    }

    public override string Identifier => "RequestMangler";

    public override string Description => 
        "This event handler can modify the parameters in the request.";

    public override IEnumerable<EventHandlerPosition> AllowedPositions => 
        new[] { EventHandlerPosition.Pre, EventHandlerPosition.Post };

    public override Dictionary<string, EventAction> Actions => new()
    {
        ["delete"] = new EventAction
        {
            Name = "delete",
            Options = new Dictionary<string, ActionOption>
            {
                ["parameter"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The parameter that should be deleted."
                }
            }
        },
        ["set"] = new EventAction
        {
            Name = "set",
            Options = new Dictionary<string, ActionOption>
            {
                ["parameter"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The parameter that should be added or modified."
                },
                ["value"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The new value of the parameter. Can contain tags like {0}, {1} for the matched sub strings."
                },
                ["match_parameter"] = new ActionOption
                {
                    Type = "str",
                    Required = false,
                    Description = "The parameter that should match some values."
                },
                ["match_pattern"] = new ActionOption
                {
                    Type = "str",
                    Required = false,
                    Description = "The value of the match_parameter. It can contain a regular expression."
                },
                ["reset_user"] = new ActionOption
                {
                    Type = "bool",
                    Required = false,
                    Description = "If the parameter is 'username', 'user' or 'realm', the user will be reset."
                }
            }
        }
    };

    public override Task<EventHandlerResult> ExecuteAsync(string action, EventHandlerOptions options)
    {
        var parameter = GetStringOption(options, "parameter");
        if (string.IsNullOrEmpty(parameter))
        {
            return Task.FromResult(new EventHandlerResult
            {
                Success = false,
                Message = "No parameter specified"
            });
        }

        var modifiedData = new Dictionary<string, object>(options.RequestData);

        switch (action.ToLowerInvariant())
        {
            case "delete":
                return ExecuteDelete(parameter, modifiedData);

            case "set":
                return ExecuteSet(parameter, options, modifiedData);

            default:
                return Task.FromResult(new EventHandlerResult
                {
                    Success = false,
                    Message = $"Unknown action: {action}"
                });
        }
    }

    private Task<EventHandlerResult> ExecuteDelete(string parameter, Dictionary<string, object> modifiedData)
    {
        if (modifiedData.ContainsKey(parameter))
        {
            modifiedData.Remove(parameter);
            _logger.LogInformation("Deleted parameter: {Parameter}", parameter);
            
            return Task.FromResult(new EventHandlerResult
            {
                Success = true,
                Message = $"Deleted parameter: {parameter}",
                ModifiedRequestData = modifiedData
            });
        }

        return Task.FromResult(new EventHandlerResult
        {
            Success = true,
            Message = $"Parameter not found: {parameter}"
        });
    }

    private Task<EventHandlerResult> ExecuteSet(
        string parameter, 
        EventHandlerOptions options, 
        Dictionary<string, object> modifiedData)
    {
        var value = GetStringOption(options, "value");
        if (value == null)
        {
            return Task.FromResult(new EventHandlerResult
            {
                Success = false,
                Message = "No value specified for set action"
            });
        }

        var matchParameter = GetStringOption(options, "match_parameter");
        var matchPattern = GetStringOption(options, "match_pattern");

        if (string.IsNullOrEmpty(matchParameter))
        {
            // Simple set - just set the parameter to the value
            modifiedData[parameter] = value;
            _logger.LogInformation("Set parameter {Parameter} to {Value}", parameter, value);
        }
        else if (!string.IsNullOrEmpty(matchPattern) && 
                 modifiedData.TryGetValue(matchParameter, out var matchValue))
        {
            // Conditional set - only set if match_parameter matches the pattern
            try
            {
                var regex = new Regex($"^{matchPattern}$");
                var match = regex.Match(matchValue?.ToString() ?? "");

                if (match.Success)
                {
                    // Replace {0}, {1}, etc. with matched groups
                    var newValue = value;
                    for (int i = 0; i < match.Groups.Count; i++)
                    {
                        newValue = newValue.Replace($"{{{i}}}", match.Groups[i].Value);
                    }

                    modifiedData[parameter] = newValue;
                    _logger.LogInformation(
                        "Set parameter {Parameter} to {NewValue} (matched {MatchParameter}={MatchValue})", 
                        parameter, newValue, matchParameter, matchValue);

                    // Handle user reset if specified
                    var resetUser = GetBoolOption(options, "reset_user");
                    if (resetUser && IsUserParameter(parameter))
                    {
                        // Mark that user should be reset based on new parameters
                        modifiedData["_reset_user"] = true;
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "Pattern {Pattern} did not match {MatchParameter}={MatchValue}", 
                        matchPattern, matchParameter, matchValue);
                    
                    return Task.FromResult(new EventHandlerResult
                    {
                        Success = true,
                        Message = $"Pattern did not match, parameter not modified"
                    });
                }
            }
            catch (RegexMatchTimeoutException)
            {
                _logger.LogWarning("Regex timeout for pattern: {Pattern}", matchPattern);
                return Task.FromResult(new EventHandlerResult
                {
                    Success = false,
                    Message = "Regex pattern matching timed out"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid regex pattern: {Pattern} - {Error}", matchPattern, ex.Message);
                return Task.FromResult(new EventHandlerResult
                {
                    Success = false,
                    Message = $"Invalid regex pattern: {ex.Message}"
                });
            }
        }
        else
        {
            // match_parameter specified but not found in request data
            return Task.FromResult(new EventHandlerResult
            {
                Success = true,
                Message = $"Match parameter '{matchParameter}' not found in request"
            });
        }

        return Task.FromResult(new EventHandlerResult
        {
            Success = true,
            Message = $"Set parameter: {parameter}",
            ModifiedRequestData = modifiedData
        });
    }

    private static bool IsUserParameter(string parameter)
    {
        return parameter.Equals("realm", StringComparison.OrdinalIgnoreCase) ||
               parameter.Equals("username", StringComparison.OrdinalIgnoreCase) ||
               parameter.Equals("user", StringComparison.OrdinalIgnoreCase);
    }
}
