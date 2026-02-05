// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 NetKnights GmbH
// Based on privacyIDEA EntraIDResolver.py

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace PrivacyIdeaServer.Lib.Resolvers
{
    /// <summary>
    /// Microsoft Entra ID (Azure AD) user resolver
    /// </summary>
    public class EntraIdResolver : HttpResolver
    {
        private IConfidentialClientApplication? _msGraphApp;
        private string _clientId = string.Empty;
        private string _tenant = string.Empty;
        private string _authority = "https://login.microsoftonline.com/{tenant}";
        private string _clientSecret = string.Empty;

        public EntraIdResolver(ILogger<EntraIdResolver> logger, IHttpClientFactory httpClientFactory)
            : base(logger, httpClientFactory)
        {
            BaseUrl = "https://graph.microsoft.com/v1.0";
            AttributeMapping = new Dictionary<string, string>
            {
                { "username", "userPrincipalName" },
                { "userid", "id" },
                { "givenname", "givenName" },
                { "surname", "surname" },
                { "email", "mail" },
                { "mobile", "mobilePhone" },
                { "phone", "businessPhones" }
            };
            ReverseAttributeMapping = AttributeMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            // Set default endpoint configurations
            ConfigGetUserById = new HttpRequestConfig
            {
                Method = "GET",
                Endpoint = "/users/{userid}"
            };

            ConfigGetUserByName = new HttpRequestConfig
            {
                Method = "GET",
                Endpoint = "/users/{username}"
            };

            ConfigGetUserList = new HttpRequestConfig
            {
                Method = "GET",
                Endpoint = "/users",
                Headers = new Dictionary<string, string>
                {
                    { "ConsistencyLevel", "eventual" }
                }
            };

            ConfigCreateUser = new HttpRequestConfig
            {
                Method = "POST",
                Endpoint = "/users",
                RequestMapping = "{\"accountEnabled\": true, \"displayName\": \"{givenname} {surname}\", \"mailNickname\": \"{givenname}\", \"passwordProfile\": {\"password\": \"{password}\"}}"
            };

            ConfigEditUser = new HttpRequestConfig
            {
                Method = "PATCH",
                Endpoint = "/users/{userid}"
            };

            ConfigDeleteUser = new HttpRequestConfig
            {
                Method = "DELETE",
                Endpoint = "/users/{userid}"
            };

            Wildcard = "";
        }

        /// <inheritdoc/>
        public override string GetResolverClassType() => "entraidresolver";

        /// <inheritdoc/>
        public override async Task LoadConfigAsync(Dictionary<string, object> config)
        {
            await base.LoadConfigAsync(config);

            _clientId = config.GetValueOrDefault("client_id", string.Empty)?.ToString() ?? string.Empty;
            _tenant = config.GetValueOrDefault("tenant", string.Empty)?.ToString() ?? string.Empty;
            _clientSecret = config.GetValueOrDefault("client_secret", string.Empty)?.ToString() ?? string.Empty;

            if (config.TryGetValue("authority", out var authority))
            {
                _authority = authority?.ToString() ?? _authority;
            }

            _authority = _authority.Replace("{tenant}", _tenant);

            try
            {
                _msGraphApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithClientSecret(_clientSecret)
                    .WithAuthority(new Uri(_authority))
                    .Build();

                Logger.LogInformation("Successfully configured Entra ID resolver");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to configure Entra ID application");
                throw;
            }
        }

        protected override async Task<HttpResponseMessage> SendHttpRequestAsync(
            HttpRequestConfig config,
            Dictionary<string, string> tags)
        {
            // Get access token from MSAL
            if (_msGraphApp != null)
            {
                try
                {
                    var scopes = new[] { "https://graph.microsoft.com/.default" };
                    var result = await _msGraphApp.AcquireTokenForClient(scopes).ExecuteAsync();

                    var client = HttpClientFactory.CreateClient();
                    client.BaseAddress = new Uri(BaseUrl);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {result.AccessToken}");

                    var request = new HttpRequestMessage(
                        new HttpMethod(config.Method),
                        ReplaceTagsInString(config.Endpoint, tags)
                    );

                    foreach (var header in config.Headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    if (config.Method.ToUpper() is "POST" or "PUT" or "PATCH")
                    {
                        var requestBody = ReplaceTagsInString(config.RequestMapping, tags);
                        if (!string.IsNullOrEmpty(requestBody))
                        {
                            request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
                        }
                    }

                    return await client.SendAsync(request);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to acquire token or send request to Entra ID");
                    throw;
                }
            }

            return await base.SendHttpRequestAsync(config, tags);
        }

        private ILogger Logger => (ILogger)typeof(HttpResolver)
            .GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(this)!;

        private IHttpClientFactory HttpClientFactory => (IHttpClientFactory)typeof(HttpResolver)
            .GetField("_httpClientFactory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(this)!;
    }
}
