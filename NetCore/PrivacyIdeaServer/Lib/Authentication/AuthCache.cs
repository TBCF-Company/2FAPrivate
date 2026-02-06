// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/authcache.py
// Authentication caching for performance optimization

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;
using Konscious.Security.Cryptography;
using System.Text;

namespace PrivacyIdeaServer.Lib.Authentication
{
    /// <summary>
    /// Authentication cache service for improving performance by caching successful authentications
    /// Equivalent to Python's authcache.py module
    /// </summary>
    public class AuthCacheService
    {
        private readonly PrivacyIDEAContext _context;
        private readonly ILogger<AuthCacheService> _logger;
        private const int Argon2Rounds = 9;

        public AuthCacheService(PrivacyIDEAContext context, ILogger<AuthCacheService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Hash password using Argon2
        /// Equivalent to Python's _hash_password using passlib.hash.argon2
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <returns>Hashed password</returns>
        private async Task<string> HashPasswordAsync(string password)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                DegreeOfParallelism = 8,
                MemorySize = 65536, // 65536 KB (64 MB)
                Iterations = Argon2Rounds,
                Salt = GenerateSalt()
            };

            var hash = await argon2.GetBytesAsync(32);
            var salt = argon2.Salt;

            // Format: $argon2id$v=19$m=65536,t=9,p=8$<salt>$<hash>
            return $"$argon2id$v=19$m=65536,t={Argon2Rounds},p=8${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verify password against Argon2 hash
        /// </summary>
        /// <param name="hash">Stored hash</param>
        /// <param name="password">Password to verify</param>
        /// <returns>True if password matches</returns>
        private async Task<bool> VerifyPasswordAsync(string hash, string password)
        {
            try
            {
                // Parse the hash format: $argon2id$v=19$m=65536,t=9,p=8$<salt>$<hash>
                var parts = hash.Split('$');
                if (parts.Length < 6 || parts[1] != "argon2id")
                {
                    _logger.LogDebug("Invalid or old (non-argon2) hash format");
                    return false;
                }

                var salt = Convert.FromBase64String(parts[4]);
                var storedHash = Convert.FromBase64String(parts[5]);

                var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
                {
                    DegreeOfParallelism = 8,
                    MemorySize = 65536,
                    Iterations = Argon2Rounds,
                    Salt = salt
                };

                var computedHash = await argon2.GetBytesAsync(32);
                return computedHash.SequenceEqual(storedHash);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error verifying password hash");
                return false;
            }
        }

        /// <summary>
        /// Generate cryptographically secure salt
        /// </summary>
        /// <returns>16-byte salt</returns>
        private byte[] GenerateSalt()
        {
            var salt = new byte[16];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        /// <summary>
        /// Add authentication to cache
        /// Equivalent to Python's add_to_cache function
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="realm">Realm</param>
        /// <param name="resolver">Resolver</param>
        /// <param name="password">Password to hash and cache</param>
        /// <returns>Record ID</returns>
        public async Task<int> AddToCacheAsync(string username, string realm, string resolver, string password)
        {
            var firstAuth = DateTime.UtcNow;
            var authHash = await HashPasswordAsync(password);
            
            var record = new AuthCache
            {
                Username = username,
                Realm = realm,
                Resolver = resolver,
                Authentication = authHash,
                FirstAuth = firstAuth,
                LastAuth = firstAuth,
                AuthCount = 0
            };

            _logger.LogDebug("Adding record to auth cache: ({Username}, {Realm}, {Resolver}, {Hash})", 
                username, realm, resolver, authHash);

            _context.AuthCaches.Add(record);
            await _context.SaveChangesAsync();

            return record.Id;
        }

        /// <summary>
        /// Update cache entry (increment auth count and update last auth time)
        /// Equivalent to Python's update_cache function
        /// </summary>
        /// <param name="cacheId">Cache entry ID</param>
        public async Task UpdateCacheAsync(int cacheId)
        {
            var lastAuth = DateTime.UtcNow;
            
            await _context.AuthCaches
                .Where(ac => ac.Id == cacheId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ac => ac.LastAuth, lastAuth)
                    .SetProperty(ac => ac.AuthCount, ac => ac.AuthCount + 1));
        }

        /// <summary>
        /// Delete authentication cache entries
        /// Equivalent to Python's delete_from_cache function
        /// Deletes all authcache entries that match the user and either match the password,
        /// are expired, or have reached the maximum number of allowed authentications
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="realm">Realm</param>
        /// <param name="resolver">Resolver</param>
        /// <param name="password">Password to match</param>
        /// <param name="lastValidCacheTime">Oldest valid time for a cache entry</param>
        /// <param name="maxAuths">Maximum number of allowed authentications</param>
        /// <returns>Number of deleted entries</returns>
        public async Task<int> DeleteFromCacheAsync(
            string username,
            string realm,
            string resolver,
            string password,
            DateTime? lastValidCacheTime = null,
            int maxAuths = 0)
        {
            var cachedAuths = await _context.AuthCaches
                .Where(ac => ac.Username == username 
                    && ac.Realm == realm 
                    && ac.Resolver == resolver)
                .ToListAsync();

            int deletedCount = 0;

            foreach (var cachedAuth in cachedAuths)
            {
                bool deleteEntry = false;

                try
                {
                    // Check if max auths exceeded
                    if (maxAuths > 0 && cachedAuth.AuthCount >= maxAuths)
                    {
                        deleteEntry = true;
                    }
                    // Check if entry expired
                    else if (lastValidCacheTime.HasValue && cachedAuth.FirstAuth < lastValidCacheTime.Value)
                    {
                        deleteEntry = true;
                    }
                    // Check if password matches
                    else if (await VerifyPasswordAsync(cachedAuth.Authentication, password))
                    {
                        deleteEntry = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Old (non-argon2) authcache entry for user {Username}@{Realm}", 
                        username, realm);
                    // Also delete old entries with invalid format
                    deleteEntry = true;
                }

                if (deleteEntry)
                {
                    deletedCount++;
                    _context.AuthCaches.Remove(cachedAuth);
                }
            }

            if (deletedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return deletedCount;
        }

        /// <summary>
        /// Cleanup old authentication cache entries
        /// Equivalent to Python's cleanup function
        /// </summary>
        /// <param name="minutes">Age of the last_auth column in minutes</param>
        /// <returns>Number of deleted cache entries</returns>
        public async Task<int> CleanupAsync(int minutes)
        {
            var cleanupTime = DateTime.UtcNow.AddMinutes(-minutes);
            
            var deletedCount = await _context.AuthCaches
                .Where(ac => ac.LastAuth < cleanupTime)
                .ExecuteDeleteAsync();

            return deletedCount;
        }

        /// <summary>
        /// Verify if credentials are cached and valid
        /// Equivalent to Python's verify_in_cache function
        /// 
        /// NOTE: There is a potential race condition between checking auth_count and updating it.
        /// In high-concurrency scenarios, multiple requests could verify the same cache entry
        /// simultaneously when auth_count is near maxAuths. Consider implementing:
        /// 1. Database-level optimistic concurrency (RowVersion/Timestamp column)
        /// 2. SELECT FOR UPDATE with transactions (not available in all EF Core providers)
        /// 3. Application-level distributed locking (Redis, etc.)
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="realm">Realm</param>
        /// <param name="resolver">Resolver</param>
        /// <param name="password">Password to verify</param>
        /// <param name="firstAuth">Only find entries newer than this timestamp</param>
        /// <param name="lastAuth">Only find entries with last_auth newer than this timestamp</param>
        /// <param name="maxAuths">Maximum number of times the cache entry can be used</param>
        /// <returns>True if credentials are cached and valid</returns>
        public async Task<bool> VerifyInCacheAsync(
            string username,
            string realm,
            string resolver,
            string password,
            DateTime? firstAuth = null,
            DateTime? lastAuth = null,
            int maxAuths = 0)
        {
            bool result = false;

            var query = _context.AuthCaches
                .Where(ac => ac.Username == username 
                    && ac.Realm == realm 
                    && ac.Resolver == resolver);

            if (firstAuth.HasValue)
            {
                query = query.Where(ac => ac.FirstAuth > firstAuth.Value);
            }

            if (lastAuth.HasValue)
            {
                query = query.Where(ac => ac.LastAuth > lastAuth.Value);
            }

            var cachedAuths = await query.ToListAsync();

            foreach (var cachedAuth in cachedAuths)
            {
                try
                {
                    result = await VerifyPasswordAsync(cachedAuth.Authentication, password);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Old (non-argon2) authcache entry for user {Username}@{Realm}", 
                        username, realm);
                    result = false;
                }

                if (result && maxAuths > 0)
                {
                    // Check if auth_count allows this authentication
                    result = cachedAuth.AuthCount < maxAuths;
                }

                if (result)
                {
                    // Update the last_auth and auth_count
                    await UpdateCacheAsync(cachedAuth.Id);
                    break;
                }
            }

            if (!result)
            {
                // Delete older entries
                await DeleteFromCacheAsync(username, realm, resolver, password, firstAuth, maxAuths);
            }

            return result;
        }
    }
}
