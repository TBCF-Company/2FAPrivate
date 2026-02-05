// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/clientapplication.py
//
// 2016-08-30 Cornelius Kölbel <cornelius.koelbel@netknights.it>
//            Save client application information for authentication requests
//
// This code is free software; you can redistribute it and/or
// modify it under the terms of the GNU AFFERO GENERAL PUBLIC LICENSE
// License as published by the Free Software Foundation; either
// version 3 of the License, or any later version.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Lib.Applications
{
    /// <summary>
    /// Manages client application information for authentication tracking
    /// Saves and retrieves client application data from authentication requests
    /// Equivalent to Python's clientapplication.py module
    /// </summary>
    public class ClientApplicationManager
    {
        private readonly ILogger<ClientApplicationManager> _logger;
        private readonly PrivacyIDEAContext _context;
        private readonly string _nodeName;

        public ClientApplicationManager(
            ILogger<ClientApplicationManager> logger,
            PrivacyIDEAContext context,
            string? nodeName = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _nodeName = nodeName ?? GetPrivacyIdeaNode();
        }

        /// <summary>
        /// Result of client application query grouped by client type or IP
        /// </summary>
        public class ClientApplicationResult
        {
            public string Ip { get; set; } = string.Empty;
            public string? Hostname { get; set; }
            public string ClientType { get; set; } = string.Empty;
            public DateTime? LastSeen { get; set; }
        }

        /// <summary>
        /// Saves or updates client application information to the database
        /// </summary>
        /// <param name="ip">IP address of the requesting client</param>
        /// <param name="clientType">Type of the client (e.g., PAM, SAML, RADIUS)</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SaveClientApplicationAsync(string ip, string clientType)
        {
            _logger.LogDebug("Saving client application: IP={Ip}, Type={ClientType}", ip, clientType);

            try
            {
                // Validate IP address
                if (!IPAddress.TryParse(ip, out var ipAddress))
                {
                    _logger.LogWarning("Invalid IP address format: {Ip}", ip);
                    throw new ArgumentException($"Invalid IP address: {ip}", nameof(ip));
                }

                var ipString = ipAddress.ToString();
                var lastSeen = DateTime.UtcNow;

                // Check if client application already exists
                var clientApp = await _context.ClientApplications
                    .FirstOrDefaultAsync(ca =>
                        ca.Ip == ipString &&
                        ca.ClientType == clientType &&
                        ca.Node == _nodeName);

                if (clientApp != null)
                {
                    // Update existing entry
                    clientApp.LastSeen = lastSeen;
                    _logger.LogDebug("Updated existing client application: {Ip}:{ClientType}", 
                        ipString, clientType);
                }
                else
                {
                    // Create new entry
                    clientApp = new ClientApplication
                    {
                        Ip = ipString,
                        ClientType = clientType,
                        Node = _nodeName,
                        LastSeen = lastSeen,
                        // TODO: Implement hostname resolution
                        Hostname = null
                    };

                    await _context.ClientApplications.AddAsync(clientApp);
                    _logger.LogDebug("Created new client application entry: {Ip}:{ClientType}", 
                        ipString, clientType);
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Handle integrity constraint violations (e.g., race conditions)
                _logger.LogInformation(ex, 
                    "Unable to write ClientApplication entry to database: {Message}", 
                    ex.Message);
                // Don't rethrow - this is not critical
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error saving client application {Ip}:{ClientType}", 
                    ip, clientType);
                throw;
            }
        }

        /// <summary>
        /// Retrieves client application information from the database
        /// </summary>
        /// <param name="ip">Optional: Filter by IP address</param>
        /// <param name="clientType">Optional: Filter by client type</param>
        /// <param name="groupBy">Group results by "clienttype" or "ip"</param>
        /// <returns>Dictionary grouped by the specified key</returns>
        public async Task<Dictionary<string, List<ClientApplicationResult>>> GetClientApplicationsAsync(
            string? ip = null,
            string? clientType = null,
            string groupBy = "clienttype")
        {
            _logger.LogDebug(
                "Getting client applications: IP={Ip}, Type={ClientType}, GroupBy={GroupBy}",
                ip, clientType, groupBy);

            try
            {
                // Build query with grouping and aggregation
                // Group by IP, hostname, and clienttype, then get MAX(lastseen)
                var query = _context.ClientApplications.AsQueryable();

                // Apply IP filter
                if (!string.IsNullOrEmpty(ip))
                {
                    if (!IPAddress.TryParse(ip, out var ipAddress))
                    {
                        _logger.LogWarning("Invalid IP address format: {Ip}", ip);
                        throw new ArgumentException($"Invalid IP address: {ip}", nameof(ip));
                    }
                    query = query.Where(ca => ca.Ip == ipAddress.ToString());
                }

                // Apply client type filter
                if (!string.IsNullOrEmpty(clientType))
                {
                    query = query.Where(ca => ca.ClientType == clientType);
                }

                // Group by IP, hostname, and clienttype, then aggregate lastseen
                var grouped = await query
                    .GroupBy(ca => new 
                    { 
                        ca.Ip, 
                        ca.Hostname, 
                        ca.ClientType 
                    })
                    .Select(g => new ClientApplicationResult
                    {
                        Ip = g.Key.Ip,
                        Hostname = g.Key.Hostname,
                        ClientType = g.Key.ClientType,
                        LastSeen = g.Max(ca => ca.LastSeen)
                    })
                    .ToListAsync();

                // Group results by specified key
                var results = new Dictionary<string, List<ClientApplicationResult>>();

                foreach (var item in grouped)
                {
                    var key = groupBy.ToLowerInvariant() == "clienttype" 
                        ? item.ClientType 
                        : item.Ip;

                    if (!results.ContainsKey(key))
                    {
                        results[key] = new List<ClientApplicationResult>();
                    }

                    if (groupBy.ToLowerInvariant() == "clienttype")
                    {
                        results[key].Add(new ClientApplicationResult
                        {
                            Ip = item.Ip,
                            Hostname = item.Hostname,
                            LastSeen = item.LastSeen
                        });
                    }
                    else
                    {
                        results[key].Add(new ClientApplicationResult
                        {
                            Hostname = item.Hostname,
                            ClientType = item.ClientType,
                            LastSeen = item.LastSeen
                        });
                    }
                }

                _logger.LogDebug("Retrieved {Count} client application groups", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client applications");
                throw;
            }
        }

        /// <summary>
        /// Gets the current PrivacyIDEA node name
        /// </summary>
        /// <returns>Node name or hostname</returns>
        private static string GetPrivacyIdeaNode()
        {
            // TODO: Implement proper node name retrieval from configuration
            // This should match the logic in lib/config.py get_privacyidea_node()
            try
            {
                return Environment.MachineName ?? "localhost";
            }
            catch
            {
                return "localhost";
            }
        }

        /// <summary>
        /// Deletes old client application entries
        /// </summary>
        /// <param name="olderThan">Delete entries older than this date</param>
        /// <returns>Number of deleted entries</returns>
        public async Task<int> CleanupOldEntriesAsync(DateTime olderThan)
        {
            _logger.LogInformation("Cleaning up client application entries older than {Date}", 
                olderThan);

            try
            {
                var oldEntries = await _context.ClientApplications
                    .Where(ca => ca.LastSeen < olderThan)
                    .ToListAsync();

                if (oldEntries.Any())
                {
                    _context.ClientApplications.RemoveRange(oldEntries);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Deleted {Count} old client application entries", 
                        oldEntries.Count);
                    return oldEntries.Count;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old client application entries");
                throw;
            }
        }

        /// <summary>
        /// Gets statistics about client applications
        /// </summary>
        /// <returns>Dictionary with statistics</returns>
        public async Task<Dictionary<string, object>> GetStatisticsAsync()
        {
            _logger.LogDebug("Getting client application statistics");

            try
            {
                var stats = new Dictionary<string, object>();

                // Total count
                stats["total_clients"] = await _context.ClientApplications.CountAsync();

                // Count by client type
                var byType = await _context.ClientApplications
                    .GroupBy(ca => ca.ClientType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type, x => x.Count);
                stats["by_type"] = byType;

                // Count by node
                var byNode = await _context.ClientApplications
                    .GroupBy(ca => ca.Node)
                    .Select(g => new { Node = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Node, x => x.Count);
                stats["by_node"] = byNode;

                // Unique IPs
                stats["unique_ips"] = await _context.ClientApplications
                    .Select(ca => ca.Ip)
                    .Distinct()
                    .CountAsync();

                // Most recent activity
                var mostRecent = await _context.ClientApplications
                    .MaxAsync(ca => ca.LastSeen);
                stats["most_recent_activity"] = mostRecent;

                _logger.LogDebug("Retrieved client application statistics");
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client application statistics");
                throw;
            }
        }
    }

    /// <summary>
    /// Extension methods for client application detection
    /// </summary>
    public static class ClientApplicationExtensions
    {
        /// <summary>
        /// Detects client type from user agent and other request headers
        /// </summary>
        /// <param name="userAgent">User agent string</param>
        /// <param name="headers">Additional headers for detection</param>
        /// <returns>Detected client type</returns>
        public static string DetectClientType(string? userAgent, Dictionary<string, string>? headers = null)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            userAgent = userAgent.ToLowerInvariant();
            headers ??= new Dictionary<string, string>();

            // PAM client detection
            if (userAgent.Contains("pam") || userAgent.Contains("linux-pam"))
                return "PAM";

            // RADIUS client detection
            if (headers.ContainsKey("X-Radius-Client") || userAgent.Contains("radius"))
                return "RADIUS";

            // SAML client detection
            if (headers.ContainsKey("X-SAML") || userAgent.Contains("saml"))
                return "SAML";

            // Web browser detection
            if (userAgent.Contains("mozilla") || userAgent.Contains("chrome") || 
                userAgent.Contains("safari") || userAgent.Contains("firefox"))
                return "WebUI";

            // API client detection
            if (userAgent.Contains("python") || userAgent.Contains("curl") || 
                userAgent.Contains("wget") || userAgent.Contains("httpie"))
                return "API";

            return "Other";
        }
    }
}
