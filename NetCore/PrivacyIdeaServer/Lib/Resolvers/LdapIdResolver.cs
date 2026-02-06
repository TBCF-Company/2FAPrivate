// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 NetKnights GmbH
// Based on privacyIDEA LDAPIdResolver.py

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;

namespace PrivacyIdeaServer.Lib.Resolvers
{
    /// <summary>
    /// Authentication types for LDAP
    /// </summary>
    public enum LdapAuthType
    {
        Simple,
        SaslDigestMd5,
        Ntlm,
        SaslKerberos
    }

    /// <summary>
    /// LDAP user resolver
    /// </summary>
    public class LdapIdResolver : UserIdResolverBase
    {
        private readonly ILogger<LdapIdResolver> _logger;

        private string _uri = string.Empty;
        private string _baseDn = string.Empty;
        private string _bindDn = string.Empty;
        private string _bindPw = string.Empty;
        private List<string> _objectClasses = new();
        private string _dnTemplate = string.Empty;
        private int _timeout = 5;
        private int _sizeLimit = 500;
        private List<string> _loginNameAttributes = new();
        private string _searchFilter = string.Empty;
        private Dictionary<string, string> _userInfo = new();
        private List<string> _multiValueAttributes = new();
        private string _uidType = string.Empty;
        private bool _noReferrals = false;
        private bool _editable = false;
        private int _cacheTimeout = 120;
        private bool _startTls = false;
        private LdapAuthType _authType = LdapAuthType.Simple;

        private readonly Dictionary<string, CachedValue<string>> _userIdCache = new();
        private readonly Dictionary<string, CachedValue<Dictionary<string, object>>> _userInfoCache = new();

        public LdapIdResolver(ILogger<LdapIdResolver> logger)
        {
            _logger = logger;
            Updateable = true;
        }

        /// <inheritdoc/>
        public override string GetResolverClassType() => "ldapresolver";

        /// <inheritdoc/>
        public override bool Editable => _editable;

        /// <inheritdoc/>
        public override Dictionary<string, object> GetResolverClassDescriptor()
        {
            var descriptor = new Dictionary<string, object>
            {
                { "clazz", "useridresolver.LDAPIdResolver.IdResolver" },
                { "config", new Dictionary<string, string>
                    {
                        { "LDAPURI", "string" },
                        { "LDAPBASE", "string" },
                        { "BINDDN", "string" },
                        { "BINDPW", "password" },
                        { "TIMEOUT", "int" },
                        { "SIZELIMIT", "int" },
                        { "LOGINNAMEATTRIBUTE", "string" },
                        { "LDAPSEARCHFILTER", "string" },
                        { "LDAPFILTER", "string" },
                        { "USERINFO", "string" },
                        { "UIDTYPE", "string" },
                        { "NOREFERRALS", "bool" },
                        { "EDITABLE", "bool" },
                        { "CACHE_TIMEOUT", "int" },
                        { "START_TLS", "bool" }
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
        public override string GetResolverId()
        {
            var idParts = new[] { _uri, _baseDn };
            var idStr = string.Join("\0", idParts);
            var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(idStr));
            return $"ldap.{Convert.ToHexString(hashBytes).ToLower()}";
        }

        /// <inheritdoc/>
        public override async Task LoadConfigAsync(Dictionary<string, object> config)
        {
            _uri = config.GetValueOrDefault("LDAPURI", string.Empty)?.ToString() ?? string.Empty;
            _baseDn = config.GetValueOrDefault("LDAPBASE", string.Empty)?.ToString() ?? string.Empty;
            _bindDn = config.GetValueOrDefault("BINDDN", string.Empty)?.ToString() ?? string.Empty;
            _bindPw = config.GetValueOrDefault("BINDPW", string.Empty)?.ToString() ?? string.Empty;
            _uidType = config.GetValueOrDefault("UIDTYPE", "DN")?.ToString() ?? "DN";
            _searchFilter = config.GetValueOrDefault("LDAPSEARCHFILTER", string.Empty)?.ToString() ?? string.Empty;

            if (config.TryGetValue("TIMEOUT", out var timeoutObj) && int.TryParse(timeoutObj?.ToString(), out var timeout))
                _timeout = timeout;

            if (config.TryGetValue("SIZELIMIT", out var sizeLimitObj) && int.TryParse(sizeLimitObj?.ToString(), out var sizeLimit))
                _sizeLimit = sizeLimit;

            if (config.TryGetValue("CACHE_TIMEOUT", out var cacheTimeoutObj) && int.TryParse(cacheTimeoutObj?.ToString(), out var cacheTimeout))
                _cacheTimeout = cacheTimeout;

            var loginNameAttr = config.GetValueOrDefault("LOGINNAMEATTRIBUTE", "uid")?.ToString() ?? "uid";
            _loginNameAttributes = loginNameAttr.Split(',').Select(s => s.Trim()).ToList();

            _noReferrals = ResolverUtils.IsTrue(config.GetValueOrDefault("NOREFERRALS", false));
            _editable = ResolverUtils.IsTrue(config.GetValueOrDefault("EDITABLE", false));
            _startTls = ResolverUtils.IsTrue(config.GetValueOrDefault("START_TLS", false));

            // Parse user info mapping
            if (config.TryGetValue("USERINFO", out var userInfoObj))
            {
                var userInfoStr = userInfoObj?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(userInfoStr))
                {
                    var pairs = userInfoStr.Split(',');
                    foreach (var pair in pairs)
                    {
                        var parts = pair.Split(':');
                        if (parts.Length == 2)
                        {
                            _userInfo[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
            }

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override async Task<bool> CheckPassAsync(string uid, string password)
        {
            try
            {
                var dn = await GetDnAsync(uid);
                if (string.IsNullOrEmpty(dn))
                {
                    _logger.LogWarning("Cannot check password: DN not found for uid {Uid}", uid);
                    return false;
                }

                using var connection = CreateConnection();
                connection.Bind(dn, password);
                _logger.LogInformation("Successfully authenticated user {Uid}", uid);
                return true;
            }
            catch (LdapException ex)
            {
                _logger.LogWarning(ex, "Failed to authenticate user {Uid}", uid);
                return false;
            }
        }

        /// <inheritdoc/>
        public override async Task<Dictionary<string, object>> GetUserInfoAsync(string userId)
        {
            // Check cache first
            if (_cacheTimeout > 0 && _userInfoCache.TryGetValue(userId, out var cached))
            {
                if (DateTime.UtcNow < cached.Timestamp.AddSeconds(_cacheTimeout))
                {
                    _logger.LogDebug("Reading user info for {UserId} from cache", userId);
                    return cached.Value;
                }
            }

            var result = new Dictionary<string, object>();

            try
            {
                var dn = await GetDnAsync(userId);
                if (string.IsNullOrEmpty(dn))
                {
                    return result;
                }

                using var connection = CreateConnection();
                BindConnection(connection);

                var searchFilter = $"({_uidType}={EscapeFilterValue(userId)})";
                if (!string.IsNullOrEmpty(_searchFilter))
                {
                    searchFilter = $"(&{_searchFilter}{searchFilter})";
                }

                var attributes = _userInfo.Values.ToArray();
                var searchResults = connection.Search(
                    _baseDn,
                    LdapConnection.ScopeSub,
                    searchFilter,
                    attributes,
                    false
                );

                if (searchResults.HasMore())
                {
                    var entry = searchResults.Next();
                    result = ExtractUserAttributes(entry);
                }

                // Cache the result
                if (_cacheTimeout > 0)
                {
                    _userInfoCache[userId] = new CachedValue<Dictionary<string, object>>
                    {
                        Value = result,
                        Timestamp = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user info for {UserId}", userId);
            }

            return result;
        }

        /// <inheritdoc/>
        public override async Task<string> GetUsernameAsync(string userId)
        {
            var userInfo = await GetUserInfoAsync(userId);
            return userInfo.GetValueOrDefault("username")?.ToString() ?? string.Empty;
        }

        /// <inheritdoc/>
        public override async Task<string> GetUserIdAsync(string loginName)
        {
            // Check cache first
            if (_cacheTimeout > 0 && _userIdCache.TryGetValue(loginName, out var cached))
            {
                if (DateTime.UtcNow < cached.Timestamp.AddSeconds(_cacheTimeout))
                {
                    _logger.LogDebug("Reading user ID for {LoginName} from cache", loginName);
                    return cached.Value;
                }
            }

            try
            {
                using var connection = CreateConnection();
                BindConnection(connection);

                var escapedLoginName = EscapeFilterValue(loginName);
                var loginFilters = _loginNameAttributes
                    .Select(attr => $"({attr}={escapedLoginName})")
                    .ToList();

                var loginFilter = loginFilters.Count == 1
                    ? loginFilters[0]
                    : $"(|{string.Join("", loginFilters)})";

                var searchFilter = !string.IsNullOrEmpty(_searchFilter)
                    ? $"(&{_searchFilter}{loginFilter})"
                    : loginFilter;

                var searchResults = connection.Search(
                    _baseDn,
                    LdapConnection.ScopeSub,
                    searchFilter,
                    new[] { _uidType },
                    false
                );

                if (searchResults.HasMore())
                {
                    var entry = searchResults.Next();
                    var userId = GetUidFromEntry(entry);

                    // Cache the result
                    if (_cacheTimeout > 0)
                    {
                        _userIdCache[loginName] = new CachedValue<string>
                        {
                            Value = userId,
                            Timestamp = DateTime.UtcNow
                        };
                    }

                    return userId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user ID for {LoginName}", loginName);
            }

            return string.Empty;
        }

        /// <inheritdoc/>
        public override async Task<List<Dictionary<string, object>>> GetUserListAsync(
            Dictionary<string, string>? searchDict = null)
        {
            var result = new List<Dictionary<string, object>>();
            searchDict ??= new Dictionary<string, string>();

            try
            {
                using var connection = CreateConnection();
                BindConnection(connection);

                var filterParts = new List<string>();

                if (!string.IsNullOrEmpty(_searchFilter))
                {
                    filterParts.Add(_searchFilter);
                }

                foreach (var kvp in searchDict)
                {
                    if (_userInfo.TryGetValue(kvp.Key, out var ldapAttr))
                    {
                        var value = kvp.Value.Replace("*", "*");
                        filterParts.Add($"({ldapAttr}={EscapeFilterValue(value)})");
                    }
                }

                var searchFilter = filterParts.Count == 1
                    ? filterParts[0]
                    : filterParts.Count > 1
                        ? $"(&{string.Join("", filterParts)})"
                        : "(objectClass=*)";

                var attributes = _userInfo.Values.ToArray();
                var searchResults = connection.Search(
                    _baseDn,
                    LdapConnection.ScopeSub,
                    searchFilter,
                    attributes,
                    false
                );

                var count = 0;
                while (searchResults.HasMore() && count < _sizeLimit)
                {
                    var entry = searchResults.Next();
                    var userAttrs = ExtractUserAttributes(entry);
                    if (userAttrs.Count > 0)
                    {
                        result.Add(userAttrs);
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user list");
            }

            await Task.CompletedTask;
            return result;
        }

        private async Task<string> GetDnAsync(string userId)
        {
            if (_uidType.Equals("DN", StringComparison.OrdinalIgnoreCase))
            {
                return userId;
            }

            try
            {
                using var connection = CreateConnection();
                BindConnection(connection);

                var searchFilter = $"(&{_searchFilter}({_uidType}={EscapeFilterValue(userId)}))";
                var searchResults = connection.Search(
                    _baseDn,
                    LdapConnection.ScopeSub,
                    searchFilter,
                    null,
                    false
                );

                if (searchResults.HasMore())
                {
                    var entry = searchResults.Next();
                    return entry.Dn;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving DN for {UserId}", userId);
            }

            await Task.CompletedTask;
            return string.Empty;
        }

        private LdapConnection CreateConnection()
        {
            var uri = new Uri(_uri);
            var connection = new LdapConnection
            {
                SecureSocketLayer = uri.Scheme.Equals("ldaps", StringComparison.OrdinalIgnoreCase)
            };

            connection.Connect(uri.Host, uri.Port > 0 ? uri.Port : (connection.SecureSocketLayer ? 636 : 389));

            if (_startTls && !connection.SecureSocketLayer)
            {
                connection.StartTls();
            }

            return connection;
        }

        private void BindConnection(LdapConnection connection)
        {
            if (!string.IsNullOrEmpty(_bindDn))
            {
                connection.Bind(_bindDn, _bindPw);
            }
            else
            {
                connection.Bind(null, null); // Anonymous bind
            }
        }

        private string GetUidFromEntry(LdapEntry entry)
        {
            if (_uidType.Equals("DN", StringComparison.OrdinalIgnoreCase))
            {
                return entry.Dn;
            }

            var attribute = entry.GetAttribute(_uidType);
            if (attribute != null)
            {
                return attribute.StringValue;
            }

            return string.Empty;
        }

        private Dictionary<string, object> ExtractUserAttributes(LdapEntry entry)
        {
            var result = new Dictionary<string, object>();

            foreach (var kvp in _userInfo)
            {
                var piKey = kvp.Key;
                var ldapAttr = kvp.Value;

                var attribute = entry.GetAttribute(ldapAttr);
                if (attribute != null)
                {
                    if (_multiValueAttributes.Contains(ldapAttr))
                    {
                        result[piKey] = attribute.StringValueArray;
                    }
                    else
                    {
                        result[piKey] = attribute.StringValue;
                    }
                }
            }

            // Always include userid
            result["userid"] = GetUidFromEntry(entry);

            return result;
        }

        private static string EscapeFilterValue(string value)
        {
            return value
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("/", "\\2f");
        }

        private class CachedValue<T>
        {
            public T Value { get; set; } = default!;
            public DateTime Timestamp { get; set; }
        }
    }
}
