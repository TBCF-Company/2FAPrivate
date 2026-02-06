// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 NetKnights GmbH
// Based on privacyIDEA SCIMIdResolver.py

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PrivacyIdeaServer.Lib.Resolvers
{
    /// <summary>
    /// SCIM (System for Cross-domain Identity Management) user resolver
    /// </summary>
    public class ScimIdResolver : UserIdResolverBase
    {
        private readonly ILogger<ScimIdResolver> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private string _authServer = string.Empty;
        private string _resourceServer = string.Empty;
        private string _authClient = "localhost";
        private string _authSecret = string.Empty;
        private string? _accessToken;

        public ScimIdResolver(ILogger<ScimIdResolver> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc/>
        public override string GetResolverClassType() => "scimresolver";

        /// <inheritdoc/>
        public override async Task LoadConfigAsync(Dictionary<string, object> config)
        {
            _authServer = config.GetValueOrDefault("authServer", string.Empty)?.ToString() ?? string.Empty;
            _resourceServer = config.GetValueOrDefault("resourceServer", string.Empty)?.ToString() ?? string.Empty;
            _authClient = config.GetValueOrDefault("authClient", "localhost")?.ToString() ?? "localhost";
            _authSecret = config.GetValueOrDefault("authSecret", string.Empty)?.ToString() ?? string.Empty;

            // Obtain access token
            await ObtainAccessTokenAsync();
        }

        private async Task ObtainAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(_authServer))
                return;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_authClient}:{_authSecret}"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {authValue}");

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                var response = await client.PostAsync(_authServer, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    _accessToken = tokenData?["access_token"]?.ToString();
                    _logger.LogInformation("Successfully obtained SCIM access token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to obtain SCIM access token");
            }
        }

        /// <inheritdoc/>
        public override async Task<Dictionary<string, object>> GetUserInfoAsync(string userId)
        {
            if (string.IsNullOrEmpty(_accessToken))
                return new Dictionary<string, object>();

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

                var response = await client.GetAsync($"{_resourceServer}/Users/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    return FillUserSchema(user ?? new Dictionary<string, object>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SCIM user info for {UserId}", userId);
            }

            return new Dictionary<string, object>();
        }

        /// <inheritdoc/>
        public override Task<string> GetUsernameAsync(string userId)
        {
            // In SCIM, userName is typically the userId
            return Task.FromResult(userId);
        }

        /// <inheritdoc/>
        public override Task<string> GetUserIdAsync(string loginName)
        {
            // In SCIM, userName is typically the userId
            return Task.FromResult(loginName);
        }

        /// <inheritdoc/>
        public override async Task<List<Dictionary<string, object>>> GetUserListAsync(
            Dictionary<string, string>? searchDict = null)
        {
            var result = new List<Dictionary<string, object>>();

            if (string.IsNullOrEmpty(_accessToken))
                return result;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

                var response = await client.GetAsync($"{_resourceServer}/Users");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    if (data != null && data.TryGetValue("Resources", out var resourcesObj))
                    {
                        var resources = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                            resourcesObj.ToString() ?? "[]");

                        if (resources != null)
                        {
                            foreach (var user in resources)
                            {
                                result.Add(FillUserSchema(user));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SCIM user list");
            }

            await Task.CompletedTask;
            return result;
        }

        private static Dictionary<string, object> FillUserSchema(Dictionary<string, object> user)
        {
            var result = new Dictionary<string, object>
            {
                ["phone"] = string.Empty,
                ["email"] = string.Empty,
                ["mobile"] = string.Empty,
                ["username"] = user.GetValueOrDefault("userName", string.Empty) ?? string.Empty
            };

            if (user.TryGetValue("name", out var nameObj) && nameObj is Dictionary<string, object> name)
            {
                result["givenname"] = name.GetValueOrDefault("givenName", string.Empty) ?? string.Empty;
                result["surname"] = name.GetValueOrDefault("familyName", string.Empty) ?? string.Empty;
            }

            if (user.TryGetValue("phoneNumbers", out var phonesObj) &&
                phonesObj is List<Dictionary<string, object>> phones && phones.Count > 0)
            {
                result["phone"] = phones[0].GetValueOrDefault("value", string.Empty) ?? string.Empty;
            }

            if (user.TryGetValue("emails", out var emailsObj) &&
                emailsObj is List<Dictionary<string, object>> emails && emails.Count > 0)
            {
                result["email"] = emails[0].GetValueOrDefault("value", string.Empty) ?? string.Empty;
            }

            return result;
        }
    }
}
