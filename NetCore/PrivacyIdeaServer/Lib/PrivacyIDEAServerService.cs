// SPDX-License-Identifier: AGPL-3.0-or-later
// Service implementation for PrivacyIDEA server operations
// Port of Python privacyideaserver.py functions

using Microsoft.EntityFrameworkCore;
using PrivacyIdeaServer.Models;

namespace PrivacyIdeaServer.Lib
{
    public class PrivacyIDEAServerService : IPrivacyIDEAServerService
    {
        private readonly PrivacyIDEAContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PrivacyIDEAServerService> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public PrivacyIDEAServerService(
            PrivacyIDEAContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<PrivacyIDEAServerService> logger,
            ILoggerFactory loggerFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// List all privacyIDEA servers or filter by identifier/id
        /// Port of Python's list_privacyideaservers function
        /// </summary>
        public async Task<Dictionary<string, object>> ListPrivacyIDEAServersAsync(string? identifier = null, int? id = null)
        {
            var res = new Dictionary<string, object>();
            var serverList = await GetPrivacyIDEAServersAsync(identifier: identifier, id: id);
            
            foreach (var server in serverList)
            {
                res[server.Config.Identifier] = new
                {
                    id = server.Config.Id,
                    url = server.Config.Url,
                    tls = server.Config.Tls,
                    description = server.Config.Description
                };
            }
            
            return res;
        }

        /// <summary>
        /// Get a single privacyIDEA server by identifier or id
        /// Port of Python's get_privacyideaserver function
        /// </summary>
        public async Task<PrivacyIDEAServer> GetPrivacyIDEAServerAsync(string? identifier = null, int? id = null)
        {
            var serverList = await GetPrivacyIDEAServersAsync(identifier: identifier, id: id);
            
            if (serverList.Count == 0)
            {
                throw new InvalidOperationException("The specified privacyIDEA Server configuration does not exist.");
            }
            
            return serverList[0];
        }

        /// <summary>
        /// Get list of privacyIDEA servers matching criteria
        /// Port of Python's get_privacyideaservers function
        /// </summary>
        public async Task<List<PrivacyIDEAServer>> GetPrivacyIDEAServersAsync(
            string? identifier = null,
            string? url = null,
            int? id = null)
        {
            var res = new List<PrivacyIDEAServer>();
            
            IQueryable<PrivacyIDEAServerDB> query = _context.PrivacyIDEAServers;
            
            if (id.HasValue)
            {
                query = query.Where(s => s.Id == id.Value);
            }
            else if (!string.IsNullOrEmpty(identifier))
            {
                query = query.Where(s => s.Identifier == identifier);
            }
            else if (!string.IsNullOrEmpty(url))
            {
                query = query.Where(s => s.Url == url);
            }
            
            var servers = await query.ToListAsync();
            
            foreach (var serverDb in servers)
            {
                var logger = _loggerFactory.CreateLogger<PrivacyIDEAServer>();
                res.Add(new PrivacyIDEAServer(serverDb, _httpClientFactory, logger));
            }
            
            return res;
        }

        /// <summary>
        /// Add or update a privacyIDEA server configuration
        /// Port of Python's add_privacyideaserver function
        /// </summary>
        public async Task<int> AddPrivacyIDEAServerAsync(
            string identifier,
            string? url = null,
            bool tls = true,
            string description = "")
        {
            var existingServer = await _context.PrivacyIDEAServers
                .FirstOrDefaultAsync(s => s.Identifier == identifier);

            if (existingServer != null)
            {
                // Update existing server
                if (!string.IsNullOrEmpty(url))
                    existingServer.Url = url;
                
                existingServer.Tls = tls;
                
                if (!string.IsNullOrEmpty(description))
                    existingServer.Description = description;

                await _context.SaveChangesAsync();
                return existingServer.Id;
            }
            else
            {
                // Create new server
                var newServer = new PrivacyIDEAServerDB
                {
                    Identifier = identifier,
                    Url = url ?? string.Empty,
                    Tls = tls,
                    Description = description
                };
                
                _context.PrivacyIDEAServers.Add(newServer);
                await _context.SaveChangesAsync();
                
                return newServer.Id;
            }
        }

        /// <summary>
        /// Delete a privacyIDEA server by identifier
        /// Port of Python's delete_privacyideaserver function
        /// </summary>
        public async Task<int> DeletePrivacyIDEAServerAsync(string identifier)
        {
            var server = await _context.PrivacyIDEAServers
                .FirstOrDefaultAsync(s => s.Identifier == identifier);
            
            if (server == null)
            {
                throw new InvalidOperationException($"PrivacyIDEA server with identifier '{identifier}' not found.");
            }
            
            var id = server.Id;
            _context.PrivacyIDEAServers.Remove(server);
            await _context.SaveChangesAsync();
            
            return id;
        }
    }
}
