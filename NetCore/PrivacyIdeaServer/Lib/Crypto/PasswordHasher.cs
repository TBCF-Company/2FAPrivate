// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Password hashing utilities with pepper support
// Equivalent to Python's crypto.py hash_with_pepper and verify_with_pepper

using System;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;

namespace PrivacyIdeaServer.Lib.Crypto
{
    /// <summary>
    /// Password hashing and verification with pepper support
    /// Equivalent to Python's hash_with_pepper and verify_with_pepper functions
    /// </summary>
    public static class PasswordHasher
    {
        private static IConfiguration? _configuration;
        private static string? _cachedPepper;

        /// <summary>
        /// Initialize the password hasher with configuration
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Get the pepper value from configuration
        /// Equivalent to Python's get_app_config_value("PI_PEPPER")
        /// </summary>
        /// <returns>Pepper value</returns>
        private static string GetPepper()
        {
            if (_cachedPepper != null)
            {
                return _cachedPepper;
            }

            // Try to get from configuration or environment variable
            _cachedPepper = _configuration?["PI_PEPPER"] 
                ?? Environment.GetEnvironmentVariable("PI_PEPPER");

            // Security: Throw if pepper is not configured (don't use default)
            if (string.IsNullOrEmpty(_cachedPepper))
            {
                throw new InvalidOperationException(
                    "PI_PEPPER is not configured. Please set PI_PEPPER in appsettings.json " +
                    "or as an environment variable. This is required for secure password hashing.");
            }

            return _cachedPepper;
        }

        /// <summary>
        /// Hash password with salt and pepper
        /// Equivalent to Python's hash_with_pepper function
        /// Used with admins and passwordReset
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <returns>Hashed password string</returns>
        public static string HashWithPepper(string password)
        {
            var key = GetPepper();
            var pepperedPassword = key + password;
            
            // Use BCrypt with work factor 12 (similar to Python's passlib default)
            return BCrypt.Net.BCrypt.HashPassword(pepperedPassword, workFactor: 12);
        }

        /// <summary>
        /// Verify password hash with the given password and pepper
        /// Equivalent to Python's verify_with_pepper function
        /// </summary>
        /// <param name="passwordHash">The password hash to verify against</param>
        /// <param name="password">The password to verify</param>
        /// <returns>True if password matches the hash</returns>
        public static bool VerifyWithPepper(string passwordHash, string password)
        {
            password ??= string.Empty;
            var key = GetPepper();
            var pepperedPassword = key + password;

            try
            {
                return BCrypt.Net.BCrypt.Verify(pepperedPassword, passwordHash);
            }
            catch (Exception)
            {
                // Invalid hash format or other error
                return false;
            }
        }
    }
}
