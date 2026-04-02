using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PrivacyIDEA.Core.EventHandlers;

/// <summary>
/// Event handler for forwarding requests to federated PrivacyIDEA servers
/// </summary>
public class FederationEventHandler : BaseEventHandler
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FederationEventHandler(
        ILogger<FederationEventHandler> logger,
        IHttpClientFactory httpClientFactory) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override string Identifier => "Federation";

    public override string Description => 
        "This event handler can forward the request to other privacyIDEA servers";

    public override Dictionary<string, EventAction> Actions => new()
    {
        ["forward"] = new EventAction
        {
            Name = "forward",
            Options = new Dictionary<string, ActionOption>
            {
                ["privacyIDEA"] = new ActionOption
                {
                    Type = "str",
                    Required = true,
                    Description = "The remote/child privacyIDEA Server."
                },
                ["realm"] = new ActionOption
                {
                    Type = "str",
                    Required = false,
                    Description = "Change the realm name to a realm on the child privacyIDEA system."
                },
                ["resolver"] = new ActionOption
                {
                    Type = "str",
                    Required = false,
                    Description = "Change the resolver name to a resolver on the child privacyIDEA system."
                },
                ["forward_client_ip"] = new ActionOption
                {
                    Type = "bool",
                    Required = false,
                    Description = "Forward the client IP to the child privacyIDEA server."
                },
                ["forward_authorization_token"] = new ActionOption
                {
                    Type = "bool",
                    Required = false,
                    Description = "Forward the authorization header."
                }
            }
        }
    };

    public override async Task<EventHandlerResult> ExecuteAsync(string action, EventHandlerOptions options)
    {
        if (action.ToLowerInvariant() != "forward")
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Unknown action: {action}"
            };
        }

        var serverIdentifier = GetStringOption(options, "privacyIDEA");
        if (string.IsNullOrEmpty(serverIdentifier))
        {
            return new EventHandlerResult
            {
                Success = false,
                Message = "No privacyIDEA server specified"
            };
        }

        try
        {
            // Get server configuration (would come from database/service)
            var serverUrl = await GetServerUrlAsync(serverIdentifier);
            if (string.IsNullOrEmpty(serverUrl))
            {
                return new EventHandlerResult
                {
                    Success = false,
                    Message = $"Server not found: {serverIdentifier}"
                };
            }

            var client = _httpClientFactory.CreateClient();
            var requestData = new Dictionary<string, object>(options.RequestData);

            // Forward client IP if configured
            if (GetBoolOption(options, "forward_client_ip"))
            {
                requestData["client"] = options.ClientIp ?? "";
            }

            // Override realm if specified
            var realm = GetStringOption(options, "realm");
            if (!string.IsNullOrEmpty(realm))
            {
                requestData["realm"] = realm;
            }

            // Override resolver if specified
            var resolver = GetStringOption(options, "resolver");
            if (!string.IsNullOrEmpty(resolver))
            {
                requestData["resolver"] = resolver;
            }

            // Build the request
            var requestMethod = options.RequestData.TryGetValue("_method", out var method) 
                ? method.ToString() : "POST";
            var requestPath = options.RequestData.TryGetValue("_path", out var path) 
                ? path.ToString() : "/validate/check";
            
            var fullUrl = $"{serverUrl.TrimEnd('/')}{requestPath}";
            _logger.LogInformation("Forwarding {Method} request to {Url}", requestMethod, fullUrl);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(fullUrl)
            };

            // Forward authorization header if configured
            if (GetBoolOption(options, "forward_authorization_token"))
            {
                if (options.RequestData.TryGetValue("_authorization", out var authHeader))
                {
                    request.Headers.Add("PI-Authorization", authHeader.ToString());
                }
            }

            HttpResponseMessage response;

            if (requestMethod?.ToUpperInvariant() == "GET")
            {
                request.Method = HttpMethod.Get;
                var queryString = string.Join("&", 
                    GetQueryParameters(requestData).Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                if (!string.IsNullOrEmpty(queryString))
                {
                    request.RequestUri = new Uri($"{fullUrl}?{queryString}");
                }
                response = await client.SendAsync(request);
            }
            else if (requestMethod?.ToUpperInvariant() == "DELETE")
            {
                request.Method = HttpMethod.Delete;
                response = await client.SendAsync(request);
            }
            else
            {
                request.Method = HttpMethod.Post;
                var content = new FormUrlEncodedContent(GetQueryParameters(requestData));
                request.Content = content;
                response = await client.SendAsync(request);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

            // Add origin information to response
            if (responseData != null)
            {
                if (responseData.TryGetValue("detail", out var detail) && detail is JsonElement detailElement)
                {
                    var detailDict = JsonSerializer.Deserialize<Dictionary<string, object>>(detailElement.GetRawText()) 
                        ?? new Dictionary<string, object>();
                    detailDict["origin"] = fullUrl;
                    responseData["detail"] = detailDict;
                }
            }

            return new EventHandlerResult
            {
                Success = true,
                Message = $"Request forwarded to {serverIdentifier}",
                ModifiedResponseData = responseData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding request to federated server");
            return new EventHandlerResult
            {
                Success = false,
                Message = $"Error forwarding request: {ex.Message}"
            };
        }
    }

    private Dictionary<string, string> GetQueryParameters(Dictionary<string, object> data)
    {
        var result = new Dictionary<string, string>();
        foreach (var kvp in data)
        {
            // Skip internal parameters
            if (kvp.Key.StartsWith("_")) continue;
            result[kvp.Key] = kvp.Value?.ToString() ?? "";
        }
        return result;
    }

    private Task<string?> GetServerUrlAsync(string serverIdentifier)
    {
        // In a real implementation, this would fetch from the PrivacyIDEAServer table
        // For now, return null to indicate server lookup is needed
        return Task.FromResult<string?>(null);
    }
}
