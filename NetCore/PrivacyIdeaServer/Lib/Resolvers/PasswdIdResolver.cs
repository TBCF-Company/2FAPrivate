// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 NetKnights GmbH
// Based on privacyIDEA PasswdIdResolver.py

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace PrivacyIdeaServer.Lib.Resolvers
{
    /// <summary>
    /// File-based user resolver for /etc/passwd style files
    /// </summary>
    public class PasswdIdResolver : UserIdResolverBase
    {
        private readonly ILogger<PasswdIdResolver> _logger;
        private string _fileName = string.Empty;

        private readonly Dictionary<string, string> _nameDict = new();
        private readonly Dictionary<string, string> _reverseDict = new();
        private readonly Dictionary<string, string[]> _descriptionDict = new();
        private readonly Dictionary<string, string> _passDict = new();
        private readonly Dictionary<string, string> _officePhoneDict = new();
        private readonly Dictionary<string, string> _homePhoneDict = new();
        private readonly Dictionary<string, string> _surnameDict = new();
        private readonly Dictionary<string, string> _givenNameDict = new();
        private readonly Dictionary<string, string> _emailDict = new();

        private static readonly Dictionary<string, string> SearchFieldTypes = new()
        {
            { "username", "text" },
            { "userid", "numeric" },
            { "description", "text" },
            { "email", "text" }
        };

        private static readonly Dictionary<string, int> SearchFieldIndices = new()
        {
            { "username", 0 },
            { "cryptpass", 1 },
            { "userid", 2 },
            { "description", 4 },
            { "email", 4 }
        };

        public PasswdIdResolver(ILogger<PasswdIdResolver> logger)
        {
            _logger = logger;
            Name = "etc-passwd";
        }

        /// <inheritdoc/>
        public override string GetResolverClassType() => "passwdresolver";

        /// <inheritdoc/>
        public override Dictionary<string, object> GetResolverClassDescriptor()
        {
            var descriptor = new Dictionary<string, object>
            {
                { "clazz", "useridresolver.PasswdIdResolver.IdResolver" },
                { "config", new Dictionary<string, string>
                    {
                        { "fileName", "string" }
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
            return _fileName;
        }

        /// <inheritdoc/>
        public override async Task LoadConfigAsync(Dictionary<string, object> config)
        {
            if (config.TryGetValue("fileName", out var fileNameObj))
            {
                _fileName = fileNameObj.ToString() ?? string.Empty;
            }
            else if (config.TryGetValue("filename", out var filenameLowerObj))
            {
                _fileName = filenameLowerObj.ToString() ?? string.Empty;
            }

            await LoadFileAsync();
        }

        /// <summary>
        /// Load user data from the passwd file
        /// </summary>
        private async Task LoadFileAsync()
        {
            if (string.IsNullOrEmpty(_fileName))
            {
                _fileName = "/etc/passwd";
            }

            _logger.LogInformation("Loading users from file {FileName}", _fileName);

            if (!File.Exists(_fileName))
            {
                _logger.LogWarning("File {FileName} does not exist", _fileName);
                return;
            }

            var lines = await File.ReadAllLinesAsync(_fileName);

            const int nameIdx = 0;
            const int passIdx = 1;
            const int idIdx = 2;
            const int descIdx = 4;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                var fields = trimmedLine.Split(':', 8);
                if (fields.Length < 3)
                    continue;

                var username = fields[nameIdx];
                var userId = fields[idIdx];

                _nameDict[username] = userId;
                _reverseDict[userId] = username;
                _descriptionDict[userId] = fields;

                if (fields.Length > passIdx)
                {
                    _passDict[userId] = fields[passIdx];
                }

                // Parse description field for additional info
                if (fields.Length > descIdx)
                {
                    var descriptions = fields[descIdx].Split(',');
                    var fullName = descriptions.Length > 0 ? descriptions[0] : string.Empty;
                    var names = fullName.Split(' ', 2);

                    _givenNameDict[userId] = names.Length > 0 ? names[0] : string.Empty;
                    _surnameDict[userId] = names.Length > 1 ? names[1] : string.Empty;
                    _officePhoneDict[userId] = descriptions.Length > 2 ? descriptions[2] : string.Empty;
                    _homePhoneDict[userId] = descriptions.Length > 3 ? descriptions[3] : string.Empty;

                    // Extract email from additional description fields
                    _emailDict[userId] = string.Empty;
                    if (descriptions.Length > 4)
                    {
                        foreach (var field in descriptions.Skip(4))
                        {
                            var emailMatch = Regex.Match(field, @".+@.+\..+");
                            if (emailMatch.Success)
                            {
                                _emailDict[userId] = emailMatch.Value;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override Task<bool> CheckPassAsync(string uid, string password)
        {
            _logger.LogInformation("Checking password for user uid {Uid}", uid);

            if (!_passDict.TryGetValue(uid, out var cryptedPassword))
            {
                _logger.LogWarning("Failed to verify password. No encrypted password found in file");
                return Task.FromResult(false);
            }

            if (cryptedPassword is "x" or "*")
            {
                _logger.LogError("Sorry, currently no support for shadow passwords");
                throw new NotImplementedException("Shadow passwords are not supported");
            }

            try
            {
                var isValid = BCrypt.Net.BCrypt.Verify(password, cryptedPassword);
                if (isValid)
                {
                    _logger.LogInformation("Successfully authenticated user uid {Uid}", uid);
                }
                else
                {
                    _logger.LogWarning("User uid {Uid} failed to authenticate", uid);
                }
                return Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error verifying password for uid {Uid}", uid);
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public override Task<Dictionary<string, object>> GetUserInfoAsync(string userId)
        {
            var result = new Dictionary<string, object>();

            if (!_reverseDict.ContainsKey(userId))
            {
                _logger.LogDebug("User with user ID {UserId} could not be found", userId);
                return Task.FromResult(result);
            }

            if (_descriptionDict.TryGetValue(userId, out var fields))
            {
                foreach (var kvp in SearchFieldIndices)
                {
                    if (kvp.Key == "cryptpass") // Skip password
                        continue;

                    if (kvp.Value < fields.Length)
                    {
                        result[kvp.Key] = fields[kvp.Value];
                    }
                }
            }

            if (_givenNameDict.TryGetValue(userId, out var givenname))
                result["givenname"] = givenname;
            if (_surnameDict.TryGetValue(userId, out var surname))
                result["surname"] = surname;
            if (_homePhoneDict.TryGetValue(userId, out var phone))
                result["phone"] = phone;
            if (_officePhoneDict.TryGetValue(userId, out var mobile))
                result["mobile"] = mobile;
            if (_emailDict.TryGetValue(userId, out var email))
                result["email"] = email;

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public override Task<string> GetUsernameAsync(string userId)
        {
            if (_descriptionDict.TryGetValue(userId, out var fields))
            {
                var index = SearchFieldIndices["username"];
                if (index < fields.Length)
                {
                    return Task.FromResult(fields[index]);
                }
            }

            _logger.LogDebug("Username for user ID {UserId} could not be found", userId);
            return Task.FromResult(string.Empty);
        }

        /// <inheritdoc/>
        public override Task<string> GetUserIdAsync(string loginName)
        {
            if (_nameDict.TryGetValue(loginName, out var userId))
            {
                return Task.FromResult(userId);
            }
            return Task.FromResult(string.Empty);
        }

        /// <inheritdoc/>
        public override async Task<List<Dictionary<string, object>>> GetUserListAsync(
            Dictionary<string, string>? searchDict = null)
        {
            var result = new List<Dictionary<string, object>>();
            searchDict ??= new Dictionary<string, string>();

            foreach (var kvp in _descriptionDict)
            {
                var userId = kvp.Key;
                var line = kvp.Value;
                var matches = true;

                foreach (var search in searchDict)
                {
                    var searchField = search.Key;
                    var pattern = search.Value;

                    if (!SearchFieldTypes.ContainsKey(searchField))
                    {
                        matches = false;
                        break;
                    }

                    _logger.LogDebug("Searching for {SearchField}:{Pattern}", searchField, pattern);

                    if (searchField is "username" or "description" or "email")
                    {
                        matches = CheckAttribute(line, pattern, searchField);
                    }
                    else if (searchField == "userid")
                    {
                        matches = CheckUserId(line, pattern);
                    }

                    if (!matches)
                        break;
                }

                if (matches)
                {
                    var userInfo = await GetUserInfoAsync(userId);
                    if (userInfo.Count > 0)
                    {
                        result.Add(userInfo);
                    }
                }
            }

            return result;
        }

        private bool CheckAttribute(string[] line, string pattern, string attributeName)
        {
            if (!SearchFieldIndices.TryGetValue(attributeName, out var index))
            {
                _logger.LogDebug("Unknown search field: {AttributeName}", attributeName);
                return false;
            }

            var attribute = index < line.Length ? line[index] : string.Empty;
            return StringMatch(attribute, pattern);
        }

        private static bool StringMatch(string value, string pattern)
        {
            var valueLower = value.ToLower();
            var patternLower = pattern.ToLower();

            var startsWith = patternLower.StartsWith("*");
            var endsWith = patternLower.EndsWith("*");

            if (startsWith)
                patternLower = patternLower[1..];
            if (endsWith)
                patternLower = patternLower[..^1];

            if (startsWith && endsWith)
                return valueLower.Contains(patternLower);
            if (startsWith)
                return valueLower.EndsWith(patternLower);
            if (endsWith)
                return valueLower.StartsWith(patternLower);

            return valueLower == patternLower;
        }

        private bool CheckUserId(string[] line, string pattern)
        {
            if (!int.TryParse(line[SearchFieldIndices["userid"]], out var currentUserId))
            {
                return false;
            }

            var match = Regex.Match(pattern, @"^(>=|<=|>|<|=|between)(.+)$");
            if (!match.Success)
                return false;

            var op = match.Groups[1].Value;
            var val = match.Groups[2].Value.Trim();

            if (op == "between")
            {
                var parts = val.Split(',');
                if (parts.Length != 2)
                    return false;

                if (!int.TryParse(parts[0].Trim(), out var lowVal) ||
                    !int.TryParse(parts[1].Trim(), out var highVal))
                    return false;

                if (highVal < lowVal)
                    (lowVal, highVal) = (highVal, lowVal);

                return currentUserId >= lowVal && currentUserId <= highVal;
            }

            if (!int.TryParse(val, out var intVal))
                return false;

            return op switch
            {
                "=" => currentUserId == intVal,
                ">" => currentUserId > intVal,
                ">=" => currentUserId >= intVal,
                "<" => currentUserId < intVal,
                "<=" => currentUserId <= intVal,
                _ => false
            };
        }
    }
}
