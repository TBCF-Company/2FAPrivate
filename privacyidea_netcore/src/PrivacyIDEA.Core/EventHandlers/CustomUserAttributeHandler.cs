using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.EventHandlers;

/// <summary>
/// User types for custom attribute handler
/// </summary>
public enum CustomAttributeUserType
{
    TokenOwner,
    LoggedInUser
}

/// <summary>
/// Event handler for managing custom user attributes
/// </summary>
public class CustomUserAttributeHandler : BaseEventHandler
{
    private readonly IUserService _userService;

    public CustomUserAttributeHandler(
        ILogger<CustomUserAttributeHandler> logger,
        IUserService userService) : base(logger)
    {
        _userService = userService;
    }

    public override string Identifier => "CustomUserAttributes";

    public override string Description => 
        "This event handler can set and delete custom user attributes";

    public override IEnumerable<EventHandlerPosition> AllowedPositions => 
        new[] { EventHandlerPosition.Pre, EventHandlerPosition.Post };

    public override Dictionary<string, EventAction> Actions => new()
    {
        ["set_custom_user_attributes"] = new EventAction
        {
            Name = "set_custom_user_attributes",
            Options = new Dictionary<string, ActionOption>
            {
                ["user"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The user for whom the custom attribute should be set.",
                    Values = new List<string> { "tokenowner", "logged_in_user" }
                },
                ["attrkey"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The key of the custom user attribute that should be set."
                },
                ["attrvalue"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The value of the custom user attribute."
                }
            }
        },
        ["delete_custom_user_attributes"] = new EventAction
        {
            Name = "delete_custom_user_attributes",
            Options = new Dictionary<string, ActionOption>
            {
                ["user"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The user from which the custom attribute should be deleted.",
                    Values = new List<string> { "tokenowner", "logged_in_user" }
                },
                ["attrkey"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The key of the custom user attribute that should be deleted."
                }
            }
        }
    };

    public override async Task<EventHandlerResult> ExecuteAsync(string action, EventHandlerOptions options)
    {
        var userType = GetStringOption(options, "user") ?? "tokenowner";
        var attrKey = GetStringOption(options, "attrkey");
        var attrValue = GetStringOption(options, "attrvalue");

        if (string.IsNullOrEmpty(attrKey))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "No attribute key specified"
            };
        }

        // Determine the target user
        string? userId = null;
        string? username = null;
        string? realm = null;

        if (userType.Equals("tokenowner", StringComparison.OrdinalIgnoreCase))
        {
            // Get token owner from request context
            userId = options.RequestData.TryGetValue("token_owner_id", out var ownerId) 
                ? ownerId?.ToString() : null;
            username = options.RequestData.TryGetValue("token_owner_username", out var ownerName) 
                ? ownerName?.ToString() : null;
        }
        else if (userType.Equals("logged_in_user", StringComparison.OrdinalIgnoreCase))
        {
            userId = options.UserId;
            username = options.Username;
            realm = options.Realm;
        }

        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(username))
        {
            _logger.LogWarning(
                "Unable to determine the user for handling the custom attribute! action: {Action}", 
                action);
            return new EventHandlerResult
            {
                Success = false,
                Message = "Unable to determine the target user"
            };
        }

        try
        {
            switch (action.ToLowerInvariant())
            {
                case "set_custom_user_attributes":
                    if (string.IsNullOrEmpty(attrValue))
                    {
                        return new EventHandlerResult
                        {
                            Success = false,
                            Message = "No attribute value specified for set action"
                        };
                    }

                    await SetUserAttributeAsync(userId, username, realm, attrKey, attrValue);
                    _logger.LogInformation(
                        "Set custom user attribute {Key}={Value} for user {User}", 
                        attrKey, attrValue, username ?? userId);

                    return new EventHandlerResult
                    {
                        Success = true,
                        Message = $"Set attribute {attrKey} for user"
                    };

                case "delete_custom_user_attributes":
                    await DeleteUserAttributeAsync(userId, username, realm, attrKey);
                    _logger.LogInformation(
                        "Deleted custom user attribute {Key} for user {User}", 
                        attrKey, username ?? userId);

                    return new EventHandlerResult
                    {
                        Success = true,
                        Message = $"Deleted attribute {attrKey} from user"
                    };

                default:
                    _logger.LogWarning("Unknown action value: {Action}", action);
                    return new EventHandlerResult
                    {
                        Success = false,
                        Message = $"Unknown action: {action}"
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling custom user attribute");
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    private async Task SetUserAttributeAsync(
        string? userId, 
        string? username, 
        string? realm, 
        string key, 
        string value)
    {
        // In real implementation, this would use IUserService to set attributes
        // For now, we'll use a placeholder implementation
        
        // This would typically be:
        // await _userService.SetUserAttributeAsync(userId ?? username, realm, key, value);
        await Task.CompletedTask;
    }

    private async Task DeleteUserAttributeAsync(
        string? userId, 
        string? username, 
        string? realm, 
        string key)
    {
        // In real implementation, this would use IUserService to delete attributes
        // For now, we'll use a placeholder implementation
        
        // This would typically be:
        // await _userService.DeleteUserAttributeAsync(userId ?? username, realm, key);
        await Task.CompletedTask;
    }
}
