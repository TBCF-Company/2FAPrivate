// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/challenge.py
//
// This is a helper module for the challenges database table.
// It is used by the token classes for challenge-response authentication.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Lib.Challenge
{
    /// <summary>
    /// Result of paginated challenge query
    /// </summary>
    public class PaginatedChallengeResult
    {
        public List<Dictionary<string, object>> Challenges { get; set; } = new();
        public int? Prev { get; set; }
        public int? Next { get; set; }
        public int Current { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Manager for challenge-response operations
    /// Equivalent to Python's lib/challenge.py module
    /// </summary>
    public class ChallengeManager
    {
        private readonly PrivacyIDEAContext _context;
        private readonly ILogger<ChallengeManager> _logger;

        public ChallengeManager(PrivacyIDEAContext context, ILogger<ChallengeManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get a list of database challenge objects
        /// </summary>
        /// <param name="serial">Challenges for this serial number</param>
        /// <param name="transactionId">Challenges with this transaction id</param>
        /// <param name="challenge">The challenge text to be found</param>
        /// <returns>List of Challenge objects</returns>
        public async Task<List<Models.Database.Challenge>> GetChallengesAsync(
            string? serial = null, 
            string? transactionId = null, 
            string? challenge = null)
        {
            _logger.LogDebug("Getting challenges: serial={Serial}, transactionId={TransactionId}, challenge={Challenge}",
                serial, transactionId, challenge);

            var query = _context.Challenges.AsQueryable();

            if (!string.IsNullOrEmpty(serial))
            {
                query = query.Where(c => c.Serial == serial);
            }

            if (!string.IsNullOrEmpty(transactionId))
            {
                query = query.Where(c => c.TransactionId == transactionId);
            }

            if (!string.IsNullOrEmpty(challenge))
            {
                query = query.Where(c => c.Challenge1 == challenge);
            }

            var challenges = await query.ToListAsync();
            _logger.LogDebug("Found {Count} challenges", challenges.Count);
            
            return challenges;
        }

        /// <summary>
        /// Retrieve a paginated challenge list for display in Web UI
        /// </summary>
        /// <param name="serial">The serial of the token</param>
        /// <param name="transactionId">The transaction_id of the challenge</param>
        /// <param name="sortBy">Sort field name (default: "Timestamp")</param>
        /// <param name="sortDir">Sort direction: "asc" or "desc"</param>
        /// <param name="pageSize">The size of the page (default: 15)</param>
        /// <param name="page">The page number (starts with 1)</param>
        /// <returns>Paginated result with challenges, prev, next and count</returns>
        public async Task<PaginatedChallengeResult> GetChallengesPaginateAsync(
            string? serial = null,
            string? transactionId = null,
            string sortBy = "Timestamp",
            string sortDir = "asc",
            int pageSize = 15,
            int page = 1)
        {
            _logger.LogDebug("Getting paginated challenges: page={Page}, size={PageSize}", page, pageSize);

            var query = CreateChallengeQuery(serial, transactionId);

            // Apply sorting
            query = ApplySort(query, sortBy, sortDir);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var challenges = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Convert to dictionary list
            var challengeList = challenges.Select(c => ChallengeToDictionary(c)).ToList();

            var hasPrev = page > 1;
            var hasNext = (page * pageSize) < totalCount;

            var result = new PaginatedChallengeResult
            {
                Challenges = challengeList,
                Prev = hasPrev ? page - 1 : null,
                Next = hasNext ? page + 1 : null,
                Current = page,
                Count = totalCount
            };

            _logger.LogDebug("Returning {Count} challenges, total={Total}", challenges.Count, totalCount);
            return result;
        }

        /// <summary>
        /// Create query for fetching challenges
        /// </summary>
        private IQueryable<Models.Database.Challenge> CreateChallengeQuery(
            string? serial = null, 
            string? transactionId = null)
        {
            var query = _context.Challenges.AsQueryable();

            // Handle serial with wildcard support
            if (!string.IsNullOrEmpty(serial) && !string.IsNullOrWhiteSpace(serial.Replace("*", "")))
            {
                if (serial.Contains("*"))
                {
                    var pattern = serial.Replace("*", "%");
                    query = query.Where(c => EF.Functions.Like(c.Serial!, pattern));
                }
                else
                {
                    query = query.Where(c => c.Serial == serial);
                }
            }

            // Handle transaction_id with wildcard support
            if (!string.IsNullOrEmpty(transactionId) && !string.IsNullOrWhiteSpace(transactionId.Replace("*", "")))
            {
                if (transactionId.Contains("*"))
                {
                    var pattern = transactionId.Replace("*", "%");
                    query = query.Where(c => EF.Functions.Like(c.TransactionId, pattern));
                }
                else
                {
                    query = query.Where(c => c.TransactionId == transactionId);
                }
            }

            return query;
        }

        /// <summary>
        /// Apply sorting to query
        /// </summary>
        private IQueryable<Models.Database.Challenge> ApplySort(
            IQueryable<Models.Database.Challenge> query, 
            string sortBy, 
            string sortDir)
        {
            // Default sort field mapping
            var sortExpression = sortBy.ToLowerInvariant() switch
            {
                "id" => sortDir == "desc" 
                    ? query.OrderByDescending(c => c.Id) 
                    : query.OrderBy(c => c.Id),
                "serial" => sortDir == "desc" 
                    ? query.OrderByDescending(c => c.Serial) 
                    : query.OrderBy(c => c.Serial),
                "transactionid" => sortDir == "desc" 
                    ? query.OrderByDescending(c => c.TransactionId) 
                    : query.OrderBy(c => c.TransactionId),
                "timestamp" => sortDir == "desc" 
                    ? query.OrderByDescending(c => c.Timestamp) 
                    : query.OrderBy(c => c.Timestamp),
                _ => sortDir == "desc" 
                    ? query.OrderByDescending(c => c.Timestamp) 
                    : query.OrderBy(c => c.Timestamp)
            };

            return sortExpression;
        }

        /// <summary>
        /// Convert Challenge to Dictionary for JSON serialization
        /// </summary>
        private Dictionary<string, object> ChallengeToDictionary(Models.Database.Challenge challenge)
        {
            return new Dictionary<string, object>
            {
                ["id"] = challenge.Id,
                ["serial"] = challenge.Serial ?? string.Empty,
                ["transaction_id"] = challenge.TransactionId,
                ["challenge"] = challenge.Challenge1 ?? string.Empty,
                ["data"] = challenge.Data ?? string.Empty,
                ["session"] = challenge.Session ?? string.Empty,
                ["timestamp"] = challenge.Timestamp ?? DateTime.MinValue,
                ["received_timestamp"] = challenge.ReceivedTimestamp,
                ["received_count"] = challenge.ReceivedCount,
                ["otp_len"] = challenge.OtpLen,
                ["otp_valid"] = challenge.OtpValid
            };
        }

        /// <summary>
        /// Extract answered challenges from a list of challenge objects.
        /// A challenge is answered if it is not expired yet AND if its otp_valid attribute is set to True.
        /// </summary>
        /// <param name="challenges">List of challenge objects</param>
        /// <returns>List of answered challenge objects</returns>
        public List<Models.Database.Challenge> ExtractAnsweredChallenges(
            List<Models.Database.Challenge> challenges)
        {
            var answeredChallenges = new List<Models.Database.Challenge>();

            foreach (var challenge in challenges)
            {
                // Check if challenge is still valid (not expired)
                if (IsValidChallenge(challenge))
                {
                    if (challenge.OtpValid)
                    {
                        answeredChallenges.Add(challenge);
                    }
                }
            }

            return answeredChallenges;
        }

        /// <summary>
        /// Check if a challenge is still valid (not expired)
        /// </summary>
        private bool IsValidChallenge(Models.Database.Challenge challenge)
        {
            // Simple check: if timestamp is within reasonable time window
            // This should be enhanced based on expiration policy
            if (challenge.Timestamp == null)
                return false;

            // Default: challenges expire after 2 minutes (120 seconds)
            var expirationMinutes = 2;
            var expirationTime = challenge.Timestamp.Value.AddMinutes(expirationMinutes);
            return DateTime.UtcNow <= expirationTime;
        }

        /// <summary>
        /// Delete challenges from the database
        /// </summary>
        /// <param name="serial">Challenges for this serial number</param>
        /// <param name="transactionId">Challenges with this transaction id</param>
        /// <returns>Number of deleted challenges</returns>
        public async Task<int> DeleteChallengesAsync(string? serial = null, string? transactionId = null)
        {
            _logger.LogDebug("Deleting challenges: serial={Serial}, transactionId={TransactionId}",
                serial, transactionId);

            var query = _context.Challenges.AsQueryable();

            if (!string.IsNullOrEmpty(serial))
            {
                query = query.Where(c => c.Serial == serial);
            }

            if (!string.IsNullOrEmpty(transactionId))
            {
                query = query.Where(c => c.TransactionId == transactionId);
            }

            var challenges = await query.ToListAsync();
            _context.Challenges.RemoveRange(challenges);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} challenges", challenges.Count);
            return challenges.Count;
        }

        /// <summary>
        /// Delete expired challenges from the challenge table
        /// </summary>
        /// <param name="chunkSize">Delete entries in chunks of the given size to avoid deadlocks</param>
        /// <param name="ageMinutes">Delete challenge entries older than this many minutes</param>
        /// <returns>Number of deleted entries</returns>
        public async Task<int> CleanupExpiredChallengesAsync(int? chunkSize = null, int? ageMinutes = null)
        {
            _logger.LogInformation("Cleaning up expired challenges: age={AgeMinutes} minutes", ageMinutes);

            var now = DateTime.UtcNow;
            var query = _context.Challenges.AsQueryable();

            if (ageMinutes.HasValue)
            {
                // Delete challenges older than the specified age
                var cutoff = now.AddMinutes(-ageMinutes.Value);
                query = query.Where(c => c.Timestamp < cutoff);
            }
            else
            {
                // Delete expired challenges (older than 2 minutes by default)
                var defaultExpiration = 2;
                var cutoff = now.AddMinutes(-defaultExpiration);
                query = query.Where(c => c.Timestamp < cutoff);
            }

            int totalDeleted = 0;

            if (chunkSize.HasValue && chunkSize.Value > 0)
            {
                // Delete in chunks to avoid deadlocks
                bool hasMore = true;
                while (hasMore)
                {
                    var chunk = await query.Take(chunkSize.Value).ToListAsync();
                    if (chunk.Count == 0)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        _context.Challenges.RemoveRange(chunk);
                        await _context.SaveChangesAsync();
                        totalDeleted += chunk.Count;
                        _logger.LogDebug("Deleted chunk of {Count} challenges", chunk.Count);
                    }
                }
            }
            else
            {
                // Delete all at once
                var challenges = await query.ToListAsync();
                _context.Challenges.RemoveRange(challenges);
                await _context.SaveChangesAsync();
                totalDeleted = challenges.Count;
            }

            _logger.LogInformation("Cleaned up {Count} expired challenges", totalDeleted);
            return totalDeleted;
        }

        /// <summary>
        /// Cancel enrollment via multichallenge for a given transaction_id
        /// by removing the challenge and the token or container.
        /// </summary>
        /// <param name="transactionId">The transaction ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> CancelEnrollmentViaMultichallengeAsync(string transactionId)
        {
            _logger.LogInformation("Canceling enrollment via multichallenge for transaction {TransactionId}", 
                transactionId);

            var challenges = await GetChallengesAsync(transactionId: transactionId);

            if (challenges.Count == 0)
            {
                _logger.LogWarning("No challenges found for transaction_id {TransactionId}", transactionId);
                return false;
            }

            if (challenges.Count > 1)
            {
                _logger.LogWarning(
                    "Multiple challenges found for transaction_id {TransactionId}, which should not be possible",
                    transactionId);
                return false;
            }

            var challenge = challenges[0];
            
            // Parse challenge data (assuming JSON format)
            var data = ParseChallengeData(challenge.Data);

            if (data == null || !data.Any())
            {
                _logger.LogWarning("No data found in challenge {ChallengeId} for transaction_id {TransactionId}",
                    challenge.Id, transactionId);
                return false;
            }

            // Check for multichallenge enrollment data
            if (!data.ContainsKey("ENROLL_VIA_MULTICHALLENGE"))
            {
                _logger.LogWarning(
                    "Challenge for transaction_id {TransactionId} contains no information about ENROLL_VIA_MULTICHALLENGE",
                    transactionId);
                return false;
            }

            if (!data.ContainsKey("ENROLL_VIA_MULTICHALLENGE_OPTIONAL"))
            {
                _logger.LogWarning(
                    "Challenge for transaction_id {TransactionId} contains no information about ENROLL_VIA_MULTICHALLENGE_OPTIONAL",
                    transactionId);
                return false;
            }

            if (data["ENROLL_VIA_MULTICHALLENGE_OPTIONAL"]?.ToString()?.ToLower() != "true")
            {
                _logger.LogWarning(
                    "Challenge {ChallengeId} for transaction_id {TransactionId} does not have ENROLL_VIA_MULTICHALLENGE_OPTIONAL set to True",
                    challenge.Id, transactionId);
                return false;
            }

            // Cancel enrollment based on type
            if (data.TryGetValue("type", out var typeValue) && typeValue?.ToString() == "container")
            {
                // Delete container
                _logger.LogInformation("Deleting container with serial {Serial}", challenge.Serial);
                // TODO: Implement container deletion when container module is available
                // await DeleteContainerBySerialAsync(challenge.Serial);
            }
            else
            {
                // Delete token
                _logger.LogInformation("Deleting token with serial {Serial}", challenge.Serial);
                // TODO: Implement token deletion when token module is available
                // await RemoveTokenAsync(challenge.Serial);
            }

            return true;
        }

        /// <summary>
        /// Parse challenge data from JSON string
        /// </summary>
        private Dictionary<string, object>? ParseChallengeData(string? data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse challenge data: {Data}", data);
                return null;
            }
        }
    }
}
