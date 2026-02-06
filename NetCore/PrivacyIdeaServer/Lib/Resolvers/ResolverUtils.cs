// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 NetKnights GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PrivacyIdeaServer.Lib.Resolvers
{
    /// <summary>
    /// Utility methods for resolvers
    /// </summary>
    public static class ResolverUtils
    {
        /// <summary>
        /// Hash a password using various supported algorithms
        /// </summary>
        public static string HashPassword(string password, string hashType)
        {
            hashType = hashType.ToUpper();

            return hashType switch
            {
                "SHA256" => HashSha256(password),
                "SHA512" => HashSha512(password),
                "SSHA256" => HashSaltedSha256(password),
                "SSHA512" => HashSaltedSha512(password),
                "BCRYPT" => BCrypt.Net.BCrypt.HashPassword(password),
                _ => throw new NotSupportedException($"Unsupported password hash type: {hashType}")
            };
        }

        private static string HashSha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        private static string HashSha512(string input)
        {
            var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        private static string HashSaltedSha256(string input)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var passwordBytes = Encoding.UTF8.GetBytes(input);
            var combined = passwordBytes.Concat(salt).ToArray();
            var hash = SHA256.HashData(combined);
            var result = hash.Concat(salt).ToArray();
            return "{SSHA256}" + Convert.ToBase64String(result);
        }

        private static string HashSaltedSha512(string input)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var passwordBytes = Encoding.UTF8.GetBytes(input);
            var combined = passwordBytes.Concat(salt).ToArray();
            var hash = SHA512.HashData(combined);
            var result = hash.Concat(salt).ToArray();
            return "{SSHA512}" + Convert.ToBase64String(result);
        }

        /// <summary>
        /// Verify a password against a hash
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            try
            {
                // Handle BCrypt
                if (hash.StartsWith("$2"))
                {
                    return BCrypt.Net.BCrypt.Verify(password, hash);
                }

                // Handle LDAP-style salted hashes
                if (hash.StartsWith("{SSHA256}"))
                {
                    return VerifySaltedSha256(password, hash[9..]);
                }
                if (hash.StartsWith("{SSHA512}"))
                {
                    return VerifySaltedSha512(password, hash[9..]);
                }

                // Handle plain SHA hashes
                if (hash.StartsWith("{SHA256}"))
                {
                    return HashSha256(password) == hash[8..];
                }
                if (hash.StartsWith("{SHA512}"))
                {
                    return HashSha512(password) == hash[8..];
                }

                // Try BCrypt verification as fallback
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        private static bool VerifySaltedSha256(string password, string base64Hash)
        {
            var hashBytes = Convert.FromBase64String(base64Hash);
            if (hashBytes.Length < 48) // 32 bytes hash + 16 bytes salt minimum
                return false;

            var hash = hashBytes.Take(32).ToArray();
            var salt = hashBytes.Skip(32).ToArray();

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var combined = passwordBytes.Concat(salt).ToArray();
            var computedHash = SHA256.HashData(combined);

            return hash.SequenceEqual(computedHash);
        }

        private static bool VerifySaltedSha512(string password, string base64Hash)
        {
            var hashBytes = Convert.FromBase64String(base64Hash);
            if (hashBytes.Length < 80) // 64 bytes hash + 16 bytes salt minimum
                return false;

            var hash = hashBytes.Take(64).ToArray();
            var salt = hashBytes.Skip(64).ToArray();

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var combined = passwordBytes.Concat(salt).ToArray();
            var computedHash = SHA512.HashData(combined);

            return hash.SequenceEqual(computedHash);
        }

        /// <summary>
        /// Censor sensitive information in connection strings
        /// </summary>
        public static string CensorConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;

            // Replace password in connection string
            var regex = new Regex(@"(password|pwd)=([^;]+)", RegexOptions.IgnoreCase);
            return regex.Replace(connectionString, "$1=***");
        }

        /// <summary>
        /// Convert a value to unicode string safely
        /// </summary>
        public static string ConvertToUnicode(object? value)
        {
            if (value == null)
                return string.Empty;

            if (value is byte[] bytes)
                return Encoding.UTF8.GetString(bytes);

            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Check if a string value represents "true"
        /// </summary>
        public static bool IsTrue(object? value)
        {
            if (value == null)
                return false;

            var str = value.ToString()?.ToLower();
            return str is "true" or "1" or "yes" or "on";
        }

        /// <summary>
        /// Error handler for delete operations that expect 204 No Content
        /// </summary>
        public static bool DeleteUserErrorHandlingNoContent(int statusCode, ILogger logger, string userIdentifier)
        {
            if (statusCode == 204)
                return true;

            logger.LogInformation("Failed to delete user {UserIdentifier}: Status code {StatusCode}",
                userIdentifier, statusCode);
            return false;
        }
    }
}
