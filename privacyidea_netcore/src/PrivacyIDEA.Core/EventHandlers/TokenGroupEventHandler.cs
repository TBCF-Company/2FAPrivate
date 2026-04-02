using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.EventHandlers;

/// <summary>
/// Event handler for token group actions
/// </summary>
public class TokenGroupEventHandler : BaseEventHandler
{
    private readonly ITokenGroupService _tokenGroupService;

    public TokenGroupEventHandler(
        ILogger<TokenGroupEventHandler> logger,
        ITokenGroupService tokenGroupService) : base(logger)
    {
        _tokenGroupService = tokenGroupService;
    }

    public override string Identifier => "TokenGroup";

    public override string Description => 
        "This event handler can trigger actions on token groups.";

    public override IEnumerable<EventHandlerPosition> AllowedPositions => 
        new[] { EventHandlerPosition.Pre, EventHandlerPosition.Post };

    public override Dictionary<string, EventAction> Actions => new()
    {
        ["add tokengroup"] = new EventAction
        {
            Name = "add tokengroup",
            Options = new Dictionary<string, ActionOption>
            {
                ["tokengroup"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The token group to add the token to."
                }
            }
        },
        ["remove tokengroup"] = new EventAction
        {
            Name = "remove tokengroup",
            Options = new Dictionary<string, ActionOption>
            {
                ["tokengroup"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The token group to remove the token from."
                }
            }
        },
        ["create tokengroup"] = new EventAction
        {
            Name = "create tokengroup",
            Options = new Dictionary<string, ActionOption>
            {
                ["name"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "Name of the token group to create."
                },
                ["description"] = new ActionOption
                {
                    Type = "str",
                    Required = false,
                    Description = "Description of the token group."
                }
            }
        },
        ["delete tokengroup"] = new EventAction
        {
            Name = "delete tokengroup",
            Options = new Dictionary<string, ActionOption>
            {
                ["tokengroup"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The token group to delete."
                }
            }
        }
    };

    public override async Task<EventHandlerResult> ExecuteAsync(string action, EventHandlerOptions options)
    {
        switch (action.ToLowerInvariant())
        {
            case "add tokengroup":
                return await AddTokenToGroupAsync(options);

            case "remove tokengroup":
                return await RemoveTokenFromGroupAsync(options);

            case "create tokengroup":
                return await CreateTokenGroupAsync(options);

            case "delete tokengroup":
                return await DeleteTokenGroupAsync(options);

            default:
                return new EventHandlerResult
                {
                    Success = false,
                    Message = $"Unknown action: {action}"
                };
        }
    }

    private async Task<EventHandlerResult> AddTokenToGroupAsync(EventHandlerOptions options)
    {
        var tokenSerial = options.TokenSerial;
        var groupName = GetStringOption(options, "tokengroup");

        if (string.IsNullOrEmpty(tokenSerial))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Token serial is required"
            };
        }

        if (string.IsNullOrEmpty(groupName))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Token group name is required"
            };
        }

        try
        {
            var group = await _tokenGroupService.GetGroupAsync(groupName);
            if (group == null)
            {
                return new EventHandlerResult
                {
                    Success = false,
                    Message = $"Token group '{groupName}' not found"
                };
            }

            await _tokenGroupService.AddTokenToGroupAsync(groupName, tokenSerial);
            _logger.LogInformation("Added token {Token} to group {Group}", tokenSerial, groupName);

            return new EventHandlerResult
            {
                Success = true,
                Message = $"Added token {tokenSerial} to group {groupName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding token {Token} to group {Group}", tokenSerial, groupName);
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error adding token to group: {ex.Message}"
            };
        }
    }

    private async Task<EventHandlerResult> RemoveTokenFromGroupAsync(EventHandlerOptions options)
    {
        var tokenSerial = options.TokenSerial;
        var groupName = GetStringOption(options, "tokengroup");

        if (string.IsNullOrEmpty(tokenSerial))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Token serial is required"
            };
        }

        if (string.IsNullOrEmpty(groupName))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Token group name is required"
            };
        }

        try
        {
            var group = await _tokenGroupService.GetGroupAsync(groupName);
            if (group == null)
            {
                return new EventHandlerResult
                {
                    Success = false,
                    Message = $"Token group '{groupName}' not found"
                };
            }

            await _tokenGroupService.RemoveTokenFromGroupAsync(groupName, tokenSerial);
            _logger.LogInformation("Removed token {Token} from group {Group}", tokenSerial, groupName);

            return new EventHandlerResult
            {
                Success = true,
                Message = $"Removed token {tokenSerial} from group {groupName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing token {Token} from group {Group}", tokenSerial, groupName);
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error removing token from group: {ex.Message}"
            };
        }
    }

    private async Task<EventHandlerResult> CreateTokenGroupAsync(EventHandlerOptions options)
    {
        var name = GetStringOption(options, "name");
        var description = GetStringOption(options, "description");

        if (string.IsNullOrEmpty(name))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Token group name is required"
            };
        }

        try
        {
            var group = await _tokenGroupService.CreateGroupAsync(name, description);
            _logger.LogInformation("Created token group {Name}", name);

            return new EventHandlerResult
            {
                Success = true,
                Message = $"Created token group {name}",
                ModifiedResponseData = new Dictionary<string, object>
                {
                    ["tokengroup_id"] = group.Id,
                    ["tokengroup_name"] = group.Name
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating token group {Name}", name);
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error creating token group: {ex.Message}"
            };
        }
    }

    private async Task<EventHandlerResult> DeleteTokenGroupAsync(EventHandlerOptions options)
    {
        var groupName = GetStringOption(options, "tokengroup");

        if (string.IsNullOrEmpty(groupName))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Token group name is required"
            };
        }

        try
        {
            var group = await _tokenGroupService.GetGroupAsync(groupName);
            if (group == null)
            {
                return new EventHandlerResult
                {
                    Success = false,
                    Message = $"Token group '{groupName}' not found"
                };
            }

            await _tokenGroupService.DeleteGroupAsync(groupName);
            _logger.LogInformation("Deleted token group {Name}", groupName);

            return new EventHandlerResult
            {
                Success = true,
                Message = $"Deleted token group {groupName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting token group {Name}", groupName);
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error deleting token group: {ex.Message}"
            };
        }
    }
}
