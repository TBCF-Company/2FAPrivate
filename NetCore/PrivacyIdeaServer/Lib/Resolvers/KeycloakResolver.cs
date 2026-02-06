// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 NetKnights GmbH
// Based on privacyIDEA KeycloakResolver.py

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace PrivacyIdeaServer.Lib.Resolvers
{
    /// <summary>
    /// Keycloak user resolver
    /// </summary>
    public class KeycloakResolver : HttpResolver
    {
        private string _realm = string.Empty;

        public KeycloakResolver(ILogger<KeycloakResolver> logger, IHttpClientFactory httpClientFactory)
            : base(logger, httpClientFactory)
        {
            BaseUrl = "http://localhost:8080";
            AttributeMapping = new Dictionary<string, string>
            {
                { "username", "username" },
                { "userid", "id" },
                { "givenname", "firstName" },
                { "surname", "lastName" },
                { "email", "email" }
            };
            ReverseAttributeMapping = AttributeMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            // Set default endpoint configurations
            ConfigGetUserById = new HttpRequestConfig
            {
                Method = "GET",
                Endpoint = "/admin/realms/{realm}/users/{userid}"
            };

            ConfigGetUserByName = new HttpRequestConfig
            {
                Method = "GET",
                Endpoint = "/admin/realms/{realm}/users",
                RequestMapping = "{\"username\": \"{username}\", \"exact\": true}"
            };

            ConfigGetUserList = new HttpRequestConfig
            {
                Method = "GET",
                Endpoint = "/admin/realms/{realm}/users"
            };

            ConfigCreateUser = new HttpRequestConfig
            {
                Method = "POST",
                Endpoint = "/admin/realms/{realm}/users",
                RequestMapping = "{\"enabled\": true}"
            };

            ConfigEditUser = new HttpRequestConfig
            {
                Method = "PUT",
                Endpoint = "/admin/realms/{realm}/users/{userid}"
            };

            ConfigDeleteUser = new HttpRequestConfig
            {
                Method = "DELETE",
                Endpoint = "/admin/realms/{realm}/users/{userid}"
            };

            ConfigAuthorization = new HttpRequestConfig
            {
                Method = "POST",
                Endpoint = "/realms/{realm}/protocol/openid-connect/token",
                RequestMapping = "grant_type=password&client_id=admin-cli&username={username}&password={password}",
                ResponseMapping = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer {access_token}" }
                },
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/x-www-form-urlencoded" }
                }
            };

            Wildcard = "";
        }

        /// <inheritdoc/>
        public override string GetResolverClassType() => "keycloakresolver";

        /// <inheritdoc/>
        public override Dictionary<string, object> GetResolverClassDescriptor()
        {
            var descriptor = base.GetResolverClassDescriptor();
            if (descriptor.TryGetValue(GetResolverClassType(), out var typeDesc) &&
                typeDesc is Dictionary<string, object> typeDescDict)
            {
                typeDescDict["clazz"] = "useridresolver.KeycloakResolver.KeycloakResolver";

                if (typeDescDict.TryGetValue("config", out var configObj) &&
                    configObj is Dictionary<string, string> configDict)
                {
                    configDict["realm"] = "string";
                }
            }

            return descriptor;
        }

        /// <inheritdoc/>
        public override async System.Threading.Tasks.Task LoadConfigAsync(Dictionary<string, object> config)
        {
            await base.LoadConfigAsync(config);

            _realm = config.GetValueOrDefault("realm", string.Empty)?.ToString() ?? string.Empty;

            // Replace realm placeholder in all endpoint configurations
            if (ConfigGetUserById != null)
                ConfigGetUserById.Endpoint = ConfigGetUserById.Endpoint.Replace("{realm}", _realm);

            if (ConfigGetUserByName != null)
                ConfigGetUserByName.Endpoint = ConfigGetUserByName.Endpoint.Replace("{realm}", _realm);

            if (ConfigGetUserList != null)
                ConfigGetUserList.Endpoint = ConfigGetUserList.Endpoint.Replace("{realm}", _realm);

            if (ConfigCreateUser != null)
                ConfigCreateUser.Endpoint = ConfigCreateUser.Endpoint.Replace("{realm}", _realm);

            if (ConfigEditUser != null)
                ConfigEditUser.Endpoint = ConfigEditUser.Endpoint.Replace("{realm}", _realm);

            if (ConfigDeleteUser != null)
                ConfigDeleteUser.Endpoint = ConfigDeleteUser.Endpoint.Replace("{realm}", _realm);

            if (ConfigAuthorization != null)
                ConfigAuthorization.Endpoint = ConfigAuthorization.Endpoint.Replace("{realm}", _realm);
        }

        /// <inheritdoc/>
        public override async System.Threading.Tasks.Task<string> GetUserIdAsync(string loginName)
        {
            if (ConfigGetUserByName == null)
                return string.Empty;

            var tags = new Dictionary<string, string> { { "username", loginName } };
            var users = await GetUserListInternalAsync(tags, ConfigGetUserByName);

            if (users.Count == 1)
            {
                return users[0].GetValueOrDefault("userid")?.ToString() ?? string.Empty;
            }

            if (users.Count > 1)
            {
                throw new System.Exception($"Multiple users found for username '{loginName}'");
            }

            return string.Empty;
        }
    }
}
