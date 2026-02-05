// SPDX-License-Identifier: AGPL-3.0-or-later
// Service interface for PrivacyIDEA server operations

using PrivacyIdeaServer.Models;

namespace PrivacyIdeaServer.Lib
{
    public interface IPrivacyIDEAServerService
    {
        Task<Dictionary<string, object>> ListPrivacyIDEAServersAsync(string? identifier = null, int? id = null);
        Task<PrivacyIDEAServer> GetPrivacyIDEAServerAsync(string? identifier = null, int? id = null);
        Task<List<PrivacyIDEAServer>> GetPrivacyIDEAServersAsync(string? identifier = null, string? url = null, int? id = null);
        Task<int> AddPrivacyIDEAServerAsync(string identifier, string? url = null, bool tls = true, string description = "");
        Task<int> DeletePrivacyIDEAServerAsync(string identifier);
    }
}
