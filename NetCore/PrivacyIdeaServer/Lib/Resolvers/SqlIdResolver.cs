// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 NetKnights GmbH
// Based on privacyIDEA SQLIdResolver.py

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrivacyIdeaServer.Lib.Resolvers
{
    /// <summary>
    /// SQL database user resolver using Entity Framework Core
    /// </summary>
    public class SqlIdResolver : UserIdResolverBase
    {
        private readonly ILogger<SqlIdResolver> _logger;
        private readonly IDbContextFactory<UserDbContext>? _contextFactory;

        private string _server = string.Empty;
        private string _driver = string.Empty;
        private string _database = string.Empty;
        private int _port;
        private int _limit = 100;
        private string _user = string.Empty;
        private string _password = string.Empty;
        private string _table = string.Empty;
        private string _where = string.Empty;
        private string _encoding = "utf-8";
        private string _connectionParams = string.Empty;
        private string _connectionString = string.Empty;
        private int _poolSize = 10;
        private int _poolTimeout = 120;
        private bool _editable;
        private string _passwordHashType = "SSHA256";

        private Dictionary<string, string> _map = new();
        private Dictionary<string, string> _reverseMap = new();

        public SqlIdResolver(ILogger<SqlIdResolver> logger, IDbContextFactory<UserDbContext>? contextFactory = null)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            Updateable = true;
        }

        /// <inheritdoc/>
        public override string GetResolverClassType() => "sqlresolver";

        /// <inheritdoc/>
        public override bool Editable => ResolverUtils.IsTrue(_editable);

        /// <inheritdoc/>
        public override Dictionary<string, object> GetResolverClassDescriptor()
        {
            var descriptor = new Dictionary<string, object>
            {
                { "clazz", "useridresolver.SQLIdResolver.IdResolver" },
                { "config", new Dictionary<string, string>
                    {
                        { "Server", "string" },
                        { "Driver", "string" },
                        { "Database", "string" },
                        { "User", "string" },
                        { "Password", "password" },
                        { "Password_Hash_Type", "string" },
                        { "Port", "int" },
                        { "Limit", "int" },
                        { "Table", "string" },
                        { "Map", "string" },
                        { "Where", "string" },
                        { "Editable", "int" },
                        { "poolTimeout", "int" },
                        { "poolSize", "int" },
                        { "Encoding", "string" },
                        { "conParams", "string" }
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
            var idParts = new[]
            {
                _connectionString,
                _poolSize.ToString(),
                _poolTimeout.ToString()
            };
            var idStr = string.Join("\0", idParts);
            var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(idStr));
            var hexString = Convert.ToHexString(hashBytes).ToLower();
            return $"sql.{hexString}";
        }

        /// <inheritdoc/>
        public override async Task LoadConfigAsync(Dictionary<string, object> config)
        {
            _server = config.GetValueOrDefault("Server", string.Empty)?.ToString() ?? string.Empty;
            _driver = config.GetValueOrDefault("Driver", string.Empty)?.ToString() ?? string.Empty;
            _database = config.GetValueOrDefault("Database", string.Empty)?.ToString() ?? string.Empty;
            _user = config.GetValueOrDefault("User", string.Empty)?.ToString() ?? string.Empty;
            _password = config.GetValueOrDefault("Password", string.Empty)?.ToString() ?? string.Empty;
            _table = config.GetValueOrDefault("Table", string.Empty)?.ToString() ?? string.Empty;

            if (config.TryGetValue("Port", out var portObj) && int.TryParse(portObj?.ToString(), out var port))
                _port = port;

            if (config.TryGetValue("Limit", out var limitObj) && int.TryParse(limitObj?.ToString(), out var limit))
                _limit = limit;

            _editable = config.TryGetValue("Editable", out var editableObj) && ResolverUtils.IsTrue(editableObj);
            _passwordHashType = config.GetValueOrDefault("Password_Hash_Type", "SSHA256")?.ToString() ?? "SSHA256";

            if (config.TryGetValue("Map", out var mapObj))
            {
                var mapStr = mapObj?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(mapStr))
                {
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();
                    _map = deserializer.Deserialize<Dictionary<string, string>>(mapStr);
                    _reverseMap = _map.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
                }
            }

            _where = config.GetValueOrDefault("Where", string.Empty)?.ToString() ?? string.Empty;
            _encoding = config.GetValueOrDefault("Encoding", "utf-8")?.ToString() ?? "utf-8";
            _connectionParams = config.GetValueOrDefault("conParams", string.Empty)?.ToString() ?? string.Empty;

            if (config.TryGetValue("poolSize", out var poolSizeObj) && int.TryParse(poolSizeObj?.ToString(), out var poolSize))
                _poolSize = poolSize;

            if (config.TryGetValue("poolTimeout", out var poolTimeoutObj) && int.TryParse(poolTimeoutObj?.ToString(), out var poolTimeout))
                _poolTimeout = poolTimeout;

            _connectionString = CreateConnectionString();
            _logger.LogDebug("Using connection string: {ConnectionString}",
                ResolverUtils.CensorConnectionString(_connectionString));

            await Task.CompletedTask;
        }

        private string CreateConnectionString()
        {
            var port = _port > 0 ? $":{_port}" : string.Empty;
            var password = !string.IsNullOrEmpty(_password) ? $":{_password}" : string.Empty;
            var conParams = !string.IsNullOrEmpty(_connectionParams) ? $"?{_connectionParams}" : string.Empty;
            var userPart = !string.IsNullOrEmpty(_user) || !string.IsNullOrEmpty(password) ? "@" : string.Empty;

            return $"{_driver}://{_user}{password}{userPart}{_server}{port}/{_database}{conParams}";
        }

        /// <inheritdoc/>
        public override async Task<bool> CheckPassAsync(string uid, string password)
        {
            var userInfo = await GetUserInfoAsync(uid);
            if (!userInfo.TryGetValue("password", out var passwordObj))
            {
                return false;
            }

            var databasePassword = passwordObj?.ToString() ?? string.Empty;

            // Remove owncloud hash format identifier
            databasePassword = System.Text.RegularExpressions.Regex.Replace(databasePassword, @"^1\|", "");

            // Translate lowercase hash identifier to uppercase
            databasePassword = System.Text.RegularExpressions.Regex.Replace(databasePassword,
                @"^\{([a-z0-9]+)\}",
                match => $"{{{match.Groups[1].Value.ToUpper()}}}");

            return ResolverUtils.VerifyPassword(password, databasePassword);
        }

        /// <inheritdoc/>
        public override async Task<Dictionary<string, object>> GetUserInfoAsync(string userId)
        {
            var result = new Dictionary<string, object>();

            if (_contextFactory == null)
            {
                _logger.LogWarning("Database context factory not configured");
                return result;
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var query = $"SELECT * FROM {_table} WHERE {GetUserIdColumn()} = @p0";

                if (!string.IsNullOrEmpty(_where))
                {
                    query += $" AND {_where}";
                }

                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = query;
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@p0";
                parameter.Value = userId;
                command.Parameters.Add(parameter);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var value = reader.GetValue(i);

                        if (_reverseMap.TryGetValue(columnName, out var fieldName))
                        {
                            result[fieldName] = ResolverUtils.ConvertToUnicode(value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get user information for {UserId}", userId);
            }

            return result;
        }

        /// <inheritdoc/>
        public override async Task<string> GetUsernameAsync(string userId)
        {
            var info = await GetUserInfoAsync(userId);
            return info.GetValueOrDefault("username")?.ToString() ?? string.Empty;
        }

        /// <inheritdoc/>
        public override async Task<string> GetUserIdAsync(string loginName)
        {
            if (_contextFactory == null)
            {
                return string.Empty;
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var usernameColumn = GetUsernameColumn();
                var userIdColumn = GetUserIdColumn();

                var query = $"SELECT {userIdColumn} FROM {_table} WHERE {usernameColumn} = @p0";

                if (!string.IsNullOrEmpty(_where))
                {
                    query += $" AND {_where}";
                }

                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = query;
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@p0";
                parameter.Value = loginName;
                command.Parameters.Add(parameter);

                var result = await command.ExecuteScalarAsync();
                return ResolverUtils.ConvertToUnicode(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get user ID for {LoginName}", loginName);
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public override async Task<List<Dictionary<string, object>>> GetUserListAsync(
            Dictionary<string, string>? searchDict = null)
        {
            var result = new List<Dictionary<string, object>>();
            searchDict ??= new Dictionary<string, string>();

            if (_contextFactory == null)
            {
                return result;
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var query = $"SELECT * FROM {_table}";
                var conditions = new List<string>();

                foreach (var kvp in searchDict)
                {
                    if (_map.TryGetValue(kvp.Key, out var column))
                    {
                        var value = kvp.Value.Replace("*", "%");
                        conditions.Add($"{column} LIKE '%{value}%'");
                    }
                }

                if (!string.IsNullOrEmpty(_where))
                {
                    conditions.Add(_where);
                }

                if (conditions.Any())
                {
                    query += " WHERE " + string.Join(" AND ", conditions);
                }

                query += $" LIMIT {_limit}";

                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = query;

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var user = new Dictionary<string, object>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var value = reader.GetValue(i);

                        if (_reverseMap.TryGetValue(columnName, out var fieldName))
                        {
                            if (fieldName != "password")
                            {
                                user[fieldName] = ResolverUtils.ConvertToUnicode(value);
                            }
                        }
                    }

                    if (user.ContainsKey("userid"))
                    {
                        result.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get user list");
            }

            return result;
        }

        /// <inheritdoc/>
        public override async Task<string?> AddUserAsync(Dictionary<string, object>? attributes = null)
        {
            if (!Editable || _contextFactory == null)
                return null;

            attributes ??= new Dictionary<string, object>();

            try
            {
                var dbAttributes = PrepareAttributesForDb(attributes);

                await using var context = await _contextFactory.CreateDbContextAsync();
                var columns = string.Join(", ", dbAttributes.Keys);
                var values = string.Join(", ", dbAttributes.Keys.Select((_, i) => $"@p{i}"));
                var query = $"INSERT INTO {_table} ({columns}) VALUES ({values}); SELECT last_insert_rowid();";

                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = query;

                var paramIndex = 0;
                foreach (var value in dbAttributes.Values)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@p{paramIndex++}";
                    parameter.Value = value;
                    command.Parameters.Add(parameter);
                }

                var result = await command.ExecuteScalarAsync();
                _logger.LogInformation("Inserted new user with ID {UserId}", result);
                return result?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user");
                return null;
            }
        }

        /// <inheritdoc/>
        public override async Task<bool> DeleteUserAsync(string uid)
        {
            if (!Editable || _contextFactory == null)
                return false;

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var userIdColumn = GetUserIdColumn();
                var query = $"DELETE FROM {_table} WHERE {userIdColumn} = @p0";

                if (!string.IsNullOrEmpty(_where))
                {
                    query += $" AND {_where}";
                }

                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = query;
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@p0";
                parameter.Value = uid;
                command.Parameters.Add(parameter);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Deleted user with uid: {Uid}", uid);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with uid {Uid}", uid);
                return false;
            }
        }

        /// <inheritdoc/>
        public override async Task<bool> UpdateUserAsync(string uid, Dictionary<string, object>? attributes = null)
        {
            if (!Editable || _contextFactory == null)
                return false;

            attributes ??= new Dictionary<string, object>();

            try
            {
                var dbAttributes = PrepareAttributesForDb(attributes);

                await using var context = await _contextFactory.CreateDbContextAsync();
                var sets = string.Join(", ", dbAttributes.Keys.Select((key, i) => $"{key} = @p{i}"));
                var userIdColumn = GetUserIdColumn();
                var query = $"UPDATE {_table} SET {sets} WHERE {userIdColumn} = @pUid";

                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = query;

                var paramIndex = 0;
                foreach (var value in dbAttributes.Values)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@p{paramIndex++}";
                    parameter.Value = value;
                    command.Parameters.Add(parameter);
                }

                var uidParameter = command.CreateParameter();
                uidParameter.ParameterName = "@pUid";
                uidParameter.Value = uid;
                command.Parameters.Add(uidParameter);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Updated user attributes for user with uid {Uid}", uid);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with uid {Uid}", uid);
                return false;
            }
        }

        private Dictionary<string, object> PrepareAttributesForDb(Dictionary<string, object> attributes)
        {
            var result = new Dictionary<string, object>();

            foreach (var kvp in attributes)
            {
                if (_map.TryGetValue(kvp.Key, out var column))
                {
                    var value = kvp.Value;

                    // Hash password if present
                    if (kvp.Key == "password" && value is string password)
                    {
                        value = ResolverUtils.HashPassword(password, _passwordHashType);
                    }

                    result[column] = value;
                }
            }

            return result;
        }

        private string GetUserIdColumn()
        {
            return _map.GetValueOrDefault("userid", "id") ?? "id";
        }

        private string GetUsernameColumn()
        {
            return _map.GetValueOrDefault("username", "username") ?? "username";
        }

        /// <inheritdoc/>
        public override async Task<(bool Success, string Description)> TestConnectionAsync(Dictionary<string, object> param)
        {
            try
            {
                var connectionString = CreateConnectionString();
                _logger.LogInformation("Testing connection to database");

                // This is a simplified test - in production, you would actually test the connection
                await Task.CompletedTask;
                return (true, "Connection test not fully implemented");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to test connection");
                return (false, $"Failed to test connection: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// DbContext for user database (placeholder - should be configured per deployment)
    /// </summary>
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
        }
    }
}
