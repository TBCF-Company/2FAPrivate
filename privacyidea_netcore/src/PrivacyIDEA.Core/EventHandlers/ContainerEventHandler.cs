using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.EventHandlers;

/// <summary>
/// Event handler for container-related actions
/// </summary>
public class ContainerEventHandler : BaseEventHandler
{
    private readonly IContainerService _containerService;
    private readonly ITokenService _tokenService;

    public ContainerEventHandler(
        ILogger<ContainerEventHandler> logger,
        IContainerService containerService,
        ITokenService tokenService) : base(logger)
    {
        _containerService = containerService;
        _tokenService = tokenService;
    }

    public override string Identifier => "Container";

    public override string Description => 
        "This event handler can trigger new actions on containers.";

    public override IEnumerable<EventHandlerPosition> AllowedPositions => 
        new[] { EventHandlerPosition.Pre, EventHandlerPosition.Post };

    public override Dictionary<string, EventAction> Actions => new()
    {
        ["create"] = new EventAction
        {
            Name = "create",
            Options = new Dictionary<string, ActionOption>
            {
                ["type"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "Container type to create",
                    Values = new List<string> { "generic", "smartphone", "yubikey" }
                },
                ["description"] = new ActionOption
                {
                    Type = "str",
                    Required = false,
                    Description = "Description of the container"
                },
                ["user"] = new ActionOption
                {
                    Type = "bool",
                    Required = false,
                    Description = "Assign container to user in request or to token/container owner"
                },
                ["token"] = new ActionOption
                {
                    Type = "bool",
                    Required = false,
                    Description = "Add token from request to container"
                }
            }
        },
        ["delete"] = new EventAction
        {
            Name = "delete",
            Options = new Dictionary<string, ActionOption>()
        },
        ["unassign"] = new EventAction
        {
            Name = "unassign",
            Options = new Dictionary<string, ActionOption>()
        },
        ["assign"] = new EventAction
        {
            Name = "assign",
            Options = new Dictionary<string, ActionOption>()
        },
        ["set states"] = new EventAction
        {
            Name = "set states",
            Options = new Dictionary<string, ActionOption>
            {
                ["active"] = new ActionOption
                {
                    Type = "bool",
                    Required = false,
                    Description = "Set the state active"
                },
                ["disabled"] = new ActionOption
                {
                    Type = "bool",
                    Required = false,
                    Description = "Set the state disabled"
                },
                ["lost"] = new ActionOption
                {
                    Type = "bool",
                    Required = false,
                    Description = "Set the state lost"
                },
                ["damaged"] = new ActionOption
                {
                    Type = "bool",
                    Required = false,
                    Description = "Set the state damaged"
                }
            }
        },
        ["set description"] = new EventAction
        {
            Name = "set description",
            Options = new Dictionary<string, ActionOption>
            {
                ["description"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "Description of the container"
                }
            }
        },
        ["remove all tokens"] = new EventAction
        {
            Name = "remove all tokens",
            Options = new Dictionary<string, ActionOption>()
        },
        ["disable all tokens"] = new EventAction
        {
            Name = "disable all tokens",
            Options = new Dictionary<string, ActionOption>()
        },
        ["enable all tokens"] = new EventAction
        {
            Name = "enable all tokens",
            Options = new Dictionary<string, ActionOption>()
        }
    };

    public override async Task<EventHandlerResult> ExecuteAsync(string action, EventHandlerOptions options)
    {
        var containerSerial = GetContainerSerial(options);

        switch (action.ToLowerInvariant())
        {
            case "create":
                return await CreateContainerAsync(options);

            case "delete":
                return await DeleteContainerAsync(containerSerial);

            case "unassign":
                return await UnassignContainerAsync(containerSerial);

            case "assign":
                return await AssignContainerAsync(containerSerial, options);

            case "set states":
                return await SetContainerStatesAsync(containerSerial, options);

            case "set description":
                return await SetContainerDescriptionAsync(containerSerial, options);

            case "remove all tokens":
                return await RemoveAllTokensAsync(containerSerial);

            case "disable all tokens":
                return await SetTokensEnabledAsync(containerSerial, false);

            case "enable all tokens":
                return await SetTokensEnabledAsync(containerSerial, true);

            default:
                return new EventHandlerResult
                {
                    Success = false,
                    Message = $"Unknown action: {action}"
                };
        }
    }

    private string? GetContainerSerial(EventHandlerOptions options)
    {
        if (options.RequestData.TryGetValue("container_serial", out var serial))
        {
            return serial?.ToString();
        }
        if (options.ResponseData.TryGetValue("container_serial", out serial))
        {
            return serial?.ToString();
        }
        return null;
    }

    private async Task<EventHandlerResult> CreateContainerAsync(EventHandlerOptions options)
    {
        var containerType = GetStringOption(options, "type");
        if (string.IsNullOrEmpty(containerType))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Container type is required for create action"
            };
        }

        var description = GetStringOption(options, "description");
        var assignUser = GetBoolOption(options, "user");
        var addToken = GetBoolOption(options, "token");

        try
        {
            var request = new CreateContainerRequest
            {
                Type = containerType,
                Description = description,
                UserId = assignUser ? options.UserId : null,
                Realm = assignUser ? options.Realm : null
            };

            var container = await _containerService.CreateContainerAsync(request);

            _logger.LogInformation("Created container {Serial} of type {Type}", 
                container.Serial, containerType);

            if (addToken && options.TokenSerial != null)
            {
                await _containerService.AddTokenToContainerAsync(container.Serial, options.TokenSerial);
                _logger.LogInformation("Added token {Token} to container {Container}", 
                    options.TokenSerial, container.Serial);
            }

            return new EventHandlerResult
            {
                Success = true,
                Message = $"Created container {container.Serial}",
                ModifiedResponseData = new Dictionary<string, object>
                {
                    ["container_serial"] = container.Serial
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating container");
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error creating container: {ex.Message}"
            };
        }
    }

    private async Task<EventHandlerResult> DeleteContainerAsync(string? serial)
    {
        if (string.IsNullOrEmpty(serial))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Container serial is required for delete action"
            };
        }

        try
        {
            await _containerService.DeleteContainerAsync(serial);
            _logger.LogInformation("Deleted container {Serial}", serial);

            return new EventHandlerResult
            {
                Success = true,
                Message = $"Deleted container {serial}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting container {Serial}", serial);
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error deleting container: {ex.Message}"
            };
        }
    }

    private async Task<EventHandlerResult> UnassignContainerAsync(string? serial)
    {
        if (string.IsNullOrEmpty(serial))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Container serial is required for unassign action"
            };
        }

        try
        {
            var result = await _containerService.UnassignContainerAsync(serial);
            if (result)
            {
                _logger.LogInformation("Unassigned container {Serial}", serial);
                return new EventHandlerResult
                {
                    Success = true,
                    Message = $"Unassigned container {serial}"
                };
            }

            return new EventHandlerResult
            {
                Success = false,
                Message = $"Container {serial} not found or already unassigned"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning container {Serial}", serial);
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error unassigning container: {ex.Message}"
            };
        }
    }

    private async Task<EventHandlerResult> AssignContainerAsync(string? serial, EventHandlerOptions options)
    {
        if (string.IsNullOrEmpty(serial))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Container serial is required for assign action"
            };
        }

        if (string.IsNullOrEmpty(options.UserId))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "User is required for assign action"
            };
        }

        try
        {
            var result = await _containerService.AssignContainerAsync(serial, options.UserId, options.Realm);
            if (result)
            {
                _logger.LogInformation("Assigned user {User} to container {Serial}", options.UserId, serial);
                return new EventHandlerResult
                {
                    Success = true,
                    Message = $"Assigned user to container {serial}"
                };
            }

            return new EventHandlerResult
            {
                Success = false,
                Message = $"Failed to assign user to container {serial}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning user to container {Serial}", serial);
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error assigning user: {ex.Message}"
            };
        }
    }

    private async Task<EventHandlerResult> SetContainerStatesAsync(string? serial, EventHandlerOptions options)
    {
        if (string.IsNullOrEmpty(serial))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Container serial is required for set states action"
            };
        }

        var states = new List<string>();
        if (GetBoolOption(options, "active")) states.Add("active");
        if (GetBoolOption(options, "disabled")) states.Add("disabled");
        if (GetBoolOption(options, "lost")) states.Add("lost");
        if (GetBoolOption(options, "damaged")) states.Add("damaged");

        if (states.Count == 0)
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "At least one state must be specified"
            };
        }

        try
        {
            // Set only the first state (interface supports single state)
            var state = states[0];
            await _containerService.SetContainerStateAsync(serial, state);
            _logger.LogInformation("Set state {State} on container {Serial}", state, serial);

            return new EventHandlerResult
            {
                Success = true,
                Message = $"Set state on container {serial}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting state on container {Serial}", serial);
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error setting state: {ex.Message}"
            };
        }
    }

    private async Task<EventHandlerResult> SetContainerDescriptionAsync(string? serial, EventHandlerOptions options)
    {
        if (string.IsNullOrEmpty(serial))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Container serial is required for set description action"
            };
        }

        var description = GetStringOption(options, "description");
        if (description == null)
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Description is required for set description action"
            };
        }

        try
        {
            await _containerService.UpdateContainerAsync(serial, new UpdateContainerRequest
            {
                Description = description
            });
            _logger.LogInformation("Set description on container {Serial}", serial);

            return new EventHandlerResult
            {
                Success = true,
                Message = $"Set description on container {serial}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting description on container {Serial}", serial);
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error setting description: {ex.Message}"
            };
        }
    }

    private async Task<EventHandlerResult> RemoveAllTokensAsync(string? serial)
    {
        if (string.IsNullOrEmpty(serial))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "Container serial is required for remove tokens action"
            };
        }

        try
        {
            var container = await _containerService.GetContainerAsync(serial);
            if (container == null)
            {
                return new EventHandlerResult
                {
                    Success = false,
                    Message = $"Container {serial} not found"
                };
            }

            var count = 0;
            foreach (var tokenSerial in container.TokenSerials)
            {
                await _containerService.RemoveTokenFromContainerAsync(serial, tokenSerial);
                count++;
            }

            _logger.LogInformation("Removed {Count} tokens from container {Serial}", count, serial);

            return new EventHandlerResult
            {
                Success = true,
                Message = $"Removed {count} tokens from container {serial}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tokens from container {Serial}", serial);
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error removing tokens: {ex.Message}"
            };
        }
    }

    private async Task<EventHandlerResult> SetTokensEnabledAsync(string? serial, bool enabled)
    {
        if (string.IsNullOrEmpty(serial))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Container serial is required for {(enabled ? "enable" : "disable")} tokens action"
            };
        }

        try
        {
            var container = await _containerService.GetContainerAsync(serial);
            if (container == null)
            {
                return new EventHandlerResult
                {
                    Success = false,
                    Message = $"Container {serial} not found"
                };
            }

            var count = 0;
            foreach (var tokenSerial in container.TokenSerials)
            {
                await _tokenService.EnableTokenAsync(tokenSerial, enabled);
                count++;
            }

            var action = enabled ? "Enabled" : "Disabled";
            _logger.LogInformation("{Action} {Count} tokens in container {Serial}", action, count, serial);

            return new EventHandlerResult
            {
                Success = true,
                Message = $"{action} {count} tokens in container {serial}"
            };
        }
        catch (Exception ex)
        {
            var action = enabled ? "enable" : "disable";
            _logger.LogError(ex, "Error {Action} tokens in container {Serial}", action, serial);
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error {action} tokens: {ex.Message}"
            };
        }
    }
}
