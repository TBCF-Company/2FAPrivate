// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 NetKnights GmbH
// Based on privacyIDEA HTTPResolver.py

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PrivacyIdeaServer.Lib.Resolvers
{
    /// <summary>
    /// HTTP request configuration for resolver operations
    /// </summary>
    public class HttpRequestConfig
    {
        public string Method { get; set; } = "GET";
        public string Endpoint { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public string RequestMapping { get; set; } = string.Empty;
        public Dictionary<string, string> ResponseMapping { get; set; } = new();
        public bool HasErrorHandler { get; set; }
        public Dictionary<string, string> ErrorResponse { get; set; } = new();

        public static HttpRequestConfig FromDictionary(Dictionary<string, object> dict, Dictionary<string, string>? defaultHeaders = null, Dictionary<string, string>? tags = null)
        {
            var config = new HttpRequestConfig();

            if (dict.TryGetValue("method", out var method))
                config.Method = method?.ToString() ?? "GET";

            if (dict.TryGetValue("endpoint", out var endpoint))
                config.Endpoint = endpoint?.ToString() ?? string.Empty;

            // Apply tags to endpoint
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    config.Endpoint = config.Endpoint.Replace($"{{{tag.Key}}}", tag.Value);
                }
            }

            if (dict.TryGetValue("headers", out var headersObj))
            {
                if (headersObj is string headersJson)
                {
                    config.Headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson) ?? new();
                }
                else if (headersObj is Dictionary<string, string> headersDict)
                {
                    config.Headers = headersDict;
                }
            }
            else if (defaultHeaders != null)
            {
                config.Headers = new Dictionary<string, string>(defaultHeaders);
            }

            if (dict.TryGetValue("requestMapping", out var requestMapping))
            {
                config.RequestMapping = requestMapping?.ToString() ?? string.Empty;
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        config.RequestMapping = config.RequestMapping.Replace($"{{{tag.Key}}}", tag.Value);
                    }
                }
            }

            if (dict.TryGetValue("responseMapping", out var responseMapping))
            {
                if (responseMapping is string responseMappingJson)
                {
                    config.ResponseMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(responseMappingJson) ?? new();
                }
            }

            if (dict.TryGetValue("hasSpecialErrorHandler", out var hasErrorHandler))
                config.HasErrorHandler = ResolverUtils.IsTrue(hasErrorHandler);

            return config;
        }
    }

    /// <summary>
    /// HTTP-based user resolver
    /// </summary>
    public class HttpResolver : UserIdResolverBase
    {
        private readonly ILogger<HttpResolver> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        protected string BaseUrl { get; set; } = string.Empty;
        protected Dictionary<string, string> Headers { get; set; } = new();
        protected Dictionary<string, string> AttributeMapping { get; set; } = new();
        protected Dictionary<string, string> ReverseAttributeMapping { get; set; } = new();
        protected HttpRequestConfig? ConfigGetUserById { get; set; }
        protected HttpRequestConfig? ConfigGetUserByName { get; set; }
        protected HttpRequestConfig? ConfigGetUserList { get; set; }
        protected HttpRequestConfig? ConfigCreateUser { get; set; }
        protected HttpRequestConfig? ConfigEditUser { get; set; }
        protected HttpRequestConfig? ConfigDeleteUser { get; set; }
        protected HttpRequestConfig? ConfigAuthorization { get; set; }
        protected HttpRequestConfig? ConfigUserAuth { get; set; }

        protected string Username { get; set; } = string.Empty;
        protected string Password { get; set; } = string.Empty;
        protected bool VerifyTls { get; set; } = true;
        protected string? TlsCaPath { get; set; }
        protected int Timeout { get; set; } = 60;
        protected string Wildcard { get; set; } = "*";

        public HttpResolver(ILogger<HttpResolver> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            Updateable = true;
        }

        /// <inheritdoc/>
        public override string GetResolverClassType() => "httpresolver";

        /// <inheritdoc/>
        public override bool Editable => ResolverUtils.IsTrue(base.Updateable);

        /// <inheritdoc/>
        public override Dictionary<string, object> GetResolverClassDescriptor()
        {
            var descriptor = new Dictionary<string, object>
            {
                { "clazz", "useridresolver.HTTPResolver.HTTPResolver" },
                { "config", new Dictionary<string, string>
                    {
                        { "base_url", "string" },
                        { "attribute_mapping", "dict" },
                        { "config_get_user_list", "dict" },
                        { "config_get_user_by_id", "dict" },
                        { "config_get_user_by_name", "dict" },
                        { "config_create_user", "dict" },
                        { "config_edit_user", "dict" },
                        { "config_delete_user", "dict" },
                        { "config_authorization", "dict" },
                        { "config_user_auth", "dict" },
                        { "username", "string" },
                        { "password", "password" },
                        { "verify_tls", "bool" },
                        { "tls_ca_path", "string" },
                        { "timeout", "int" }
                    }
                }
            };

            var typ = GetResolverClassType();
            return new Dictionary<string, object>
            {
                { typ, descriptor }
            };
        }

        /// <inheritdoc/>
        public override async Task LoadConfigAsync(Dictionary<string, object> config)
        {
            BaseUrl = config.GetValueOrDefault("base_url", string.Empty)?.ToString() ?? string.Empty;
            Username = config.GetValueOrDefault("username", string.Empty)?.ToString() ?? string.Empty;
            Password = config.GetValueOrDefault("password", string.Empty)?.ToString() ?? string.Empty;
            VerifyTls = ResolverUtils.IsTrue(config.GetValueOrDefault("verify_tls", true));
            TlsCaPath = config.GetValueOrDefault("tls_ca_path")?.ToString();

            if (config.TryGetValue("timeout", out var timeoutObj) && int.TryParse(timeoutObj?.ToString(), out var timeout))
                Timeout = timeout;

            // Load attribute mapping
            if (config.TryGetValue("attribute_mapping", out var mappingObj))
            {
                if (mappingObj is string mappingJson)
                {
                    AttributeMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson) ?? new();
                }
                else if (mappingObj is Dictionary<string, string> mappingDict)
                {
                    AttributeMapping = mappingDict;
                }
                ReverseAttributeMapping = AttributeMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            }

            // Load headers
            if (config.TryGetValue("headers", out var headersObj))
            {
                if (headersObj is string headersJson)
                {
                    Headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson) ?? new();
                }
            }

            // Load endpoint configurations
            if (config.TryGetValue("config_get_user_by_id", out var getUserByIdObj) && getUserByIdObj is Dictionary<string, object> getUserByIdDict)
                ConfigGetUserById = HttpRequestConfig.FromDictionary(getUserByIdDict, Headers);

            if (config.TryGetValue("config_get_user_by_name", out var getUserByNameObj) && getUserByNameObj is Dictionary<string, object> getUserByNameDict)
                ConfigGetUserByName = HttpRequestConfig.FromDictionary(getUserByNameDict, Headers);

            if (config.TryGetValue("config_get_user_list", out var getUserListObj) && getUserListObj is Dictionary<string, object> getUserListDict)
                ConfigGetUserList = HttpRequestConfig.FromDictionary(getUserListDict, Headers);

            if (config.TryGetValue("config_create_user", out var createUserObj) && createUserObj is Dictionary<string, object> createUserDict)
                ConfigCreateUser = HttpRequestConfig.FromDictionary(createUserDict, Headers);

            if (config.TryGetValue("config_edit_user", out var editUserObj) && editUserObj is Dictionary<string, object> editUserDict)
                ConfigEditUser = HttpRequestConfig.FromDictionary(editUserDict, Headers);

            if (config.TryGetValue("config_delete_user", out var deleteUserObj) && deleteUserObj is Dictionary<string, object> deleteUserDict)
                ConfigDeleteUser = HttpRequestConfig.FromDictionary(deleteUserDict, Headers);

            if (config.TryGetValue("config_authorization", out var authObj) && authObj is Dictionary<string, object> authDict)
                ConfigAuthorization = HttpRequestConfig.FromDictionary(authDict, Headers);

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override async Task<string> GetUserIdAsync(string loginName)
        {
            if (ConfigGetUserByName == null)
            {
                _logger.LogDebug("No configuration to get user by name available, returning login name");
                return loginName;
            }

            var tags = new Dictionary<string, string> { { "username", loginName } };
            var userInfo = await GetUserAsync(loginName, ConfigGetUserByName, tags);
            return userInfo.GetValueOrDefault("userid")?.ToString() ?? string.Empty;
        }

        /// <inheritdoc/>
        public override async Task<string> GetUsernameAsync(string userId)
        {
            var userInfo = await GetUserInfoAsync(userId);
            return userInfo.GetValueOrDefault("username")?.ToString() ?? string.Empty;
        }

        /// <inheritdoc/>
        public override async Task<Dictionary<string, object>> GetUserInfoAsync(string userId)
        {
            if (ConfigGetUserById == null)
            {
                return new Dictionary<string, object>();
            }

            var tags = new Dictionary<string, string> { { "userid", userId } };
            return await GetUserAsync(userId, ConfigGetUserById, tags);
        }

        /// <inheritdoc/>
        public override async Task<List<Dictionary<string, object>>> GetUserListAsync(
            Dictionary<string, string>? searchDict = null)
        {
            if (ConfigGetUserList == null)
            {
                return new List<Dictionary<string, object>>();
            }

            var tags = searchDict ?? new Dictionary<string, string>();
            return await GetUserListInternalAsync(tags, ConfigGetUserList);
        }

        /// <inheritdoc/>
        public override async Task<string?> AddUserAsync(Dictionary<string, object>? attributes = null)
        {
            if (!Editable || ConfigCreateUser == null)
                return null;

            attributes ??= new Dictionary<string, object>();

            try
            {
                var tags = attributes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty
                );

                var response = await SendHttpRequestAsync(ConfigCreateUser, tags);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    if (responseObj != null && responseObj.TryGetValue("id", out var userId))
                    {
                        _logger.LogInformation("Created user with ID {UserId}", userId);
                        return userId?.ToString();
                    }
                }

                _logger.LogWarning("Failed to create user: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return null;
            }
        }

        /// <inheritdoc/>
        public override async Task<bool> DeleteUserAsync(string uid)
        {
            if (!Editable || ConfigDeleteUser == null)
                return false;

            try
            {
                var tags = new Dictionary<string, string> { { "userid", uid } };
                var response = await SendHttpRequestAsync(ConfigDeleteUser, tags);

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation("Deleted user {UserId}", uid);
                    return true;
                }

                _logger.LogWarning("Failed to delete user {UserId}: {StatusCode}", uid, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", uid);
                return false;
            }
        }

        /// <inheritdoc/>
        public override async Task<bool> UpdateUserAsync(string uid, Dictionary<string, object>? attributes = null)
        {
            if (!Editable || ConfigEditUser == null)
                return false;

            attributes ??= new Dictionary<string, object>();

            try
            {
                var tags = attributes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty
                );
                tags["userid"] = uid;

                var response = await SendHttpRequestAsync(ConfigEditUser, tags);
                var success = response.IsSuccessStatusCode;

                if (success)
                {
                    _logger.LogInformation("Updated user {UserId}", uid);
                }
                else
                {
                    _logger.LogWarning("Failed to update user {UserId}: {StatusCode}", uid, response.StatusCode);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", uid);
                return false;
            }
        }

        protected virtual async Task<Dictionary<string, object>> GetUserAsync(
            string identifier,
            HttpRequestConfig config,
            Dictionary<string, string> tags)
        {
            try
            {
                var response = await SendHttpRequestAsync(config, tags);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    return MapUserAttributes(data ?? new Dictionary<string, object>());
                }

                _logger.LogWarning("Failed to get user {Identifier}: {StatusCode}", identifier, response.StatusCode);
                return new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {Identifier}", identifier);
                return new Dictionary<string, object>();
            }
        }

        protected virtual async Task<List<Dictionary<string, object>>> GetUserListInternalAsync(
            Dictionary<string, string> tags,
            HttpRequestConfig config)
        {
            try
            {
                var response = await SendHttpRequestAsync(config, tags);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content);
                    return data?.Select(MapUserAttributes).ToList() ?? new List<Dictionary<string, object>>();
                }

                _logger.LogWarning("Failed to get user list: {StatusCode}", response.StatusCode);
                return new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user list");
                return new List<Dictionary<string, object>>();
            }
        }

        protected virtual async Task<HttpResponseMessage> SendHttpRequestAsync(
            HttpRequestConfig config,
            Dictionary<string, string> tags)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(Timeout);
            client.BaseAddress = new Uri(BaseUrl);

            var request = new HttpRequestMessage(
                new HttpMethod(config.Method),
                ReplaceTagsInString(config.Endpoint, tags)
            );

            // Add headers
            foreach (var header in config.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Add authorization if configured
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            }

            // Add request body for POST/PUT/PATCH
            if (config.Method.ToUpper() is "POST" or "PUT" or "PATCH")
            {
                var requestBody = ReplaceTagsInString(config.RequestMapping, tags);
                if (!string.IsNullOrEmpty(requestBody))
                {
                    var contentType = config.Headers.GetValueOrDefault("Content-Type", "application/json");
                    if (contentType == "application/json")
                    {
                        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    }
                    else
                    {
                        request.Content = new StringContent(requestBody, Encoding.UTF8, contentType);
                    }
                }
            }

            return await client.SendAsync(request);
        }

        protected Dictionary<string, object> MapUserAttributes(Dictionary<string, object> userStoreData)
        {
            var result = new Dictionary<string, object>();

            foreach (var kvp in userStoreData)
            {
                if (ReverseAttributeMapping.TryGetValue(kvp.Key, out var piKey))
                {
                    result[piKey] = kvp.Value;
                }
            }

            return result;
        }

        protected static string ReplaceTagsInString(string input, Dictionary<string, string> tags)
        {
            foreach (var tag in tags)
            {
                input = input.Replace($"{{{tag.Key}}}", tag.Value);
            }
            return input;
        }
    }
}
