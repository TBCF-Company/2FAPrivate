//  2023-03-15 Cornelius Kölbel <cornelius.koelbel@netknights.it>
//             Init
//  Converted to C# .NET Core 8 - 2026-02-05
//
//  License:  AGPLv3
//  contact:  http://www.privacyidea.org
//
// This code is free software; you can redistribute it and/or
// modify it under the terms of the GNU AFFERO GENERAL PUBLIC LICENSE
// License as published by the Free Software Foundation; either
// version 3 of the License, or any later version.
//
// This code is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU AFFERO GENERAL PUBLIC LICENSE for more details.
//
// You should have received a copy of the GNU Affero General Public
// License along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Lib;

/// <summary>
/// This module contains the functions to manage service ids.
/// </summary>
public class ServiceIdManager
{
    private readonly PrivacyIDEAContext _context;
    private readonly ILogger<ServiceIdManager> _logger;

    public ServiceIdManager(PrivacyIDEAContext context, ILogger<ServiceIdManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Set or update a service id.
    /// </summary>
    /// <param name="name">Name of the serviceid</param>
    /// <param name="description">Description of the serviceid</param>
    /// <returns>The ID of the service</returns>
    public async Task<int> SetServiceIdAsync(string name, string? description = null)
    {
        var serviceId = await _context.Serviceids
            .FirstOrDefaultAsync(s => s.Name == name);

        if (serviceId != null)
        {
            // Update existing
            serviceId.Description = description;
        }
        else
        {
            // Create new
            serviceId = new Serviceid
            {
                Name = name,
                Description = description
            };
            _context.Serviceids.Add(serviceId);
        }

        await _context.SaveChangesAsync();
        return serviceId.Id;
    }

    /// <summary>
    /// Delete the serviceid given by either name or id.
    /// If there are still applications with this serviceid, the function fails with an error.
    /// </summary>
    /// <param name="name">Name of the serviceid to be deleted</param>
    /// <param name="sid">ID of the serviceid to delete</param>
    public async Task DeleteServiceIdAsync(string? name = null, int? sid = null)
    {
        Serviceid? serviceId = null;

        if (!string.IsNullOrEmpty(name))
        {
            serviceId = await _context.Serviceids
                .FirstOrDefaultAsync(s => s.Name == name);
        }

        if (sid.HasValue)
        {
            if (serviceId != null && serviceId.Id != sid.Value)
            {
                throw new PrivacyIDEAError(
                    $"ID of the serviceid with name {name} does not match given ID ({sid}).");
            }

            if (serviceId == null)
            {
                serviceId = await _context.Serviceids
                    .FirstOrDefaultAsync(s => s.Id == sid.Value);
            }
        }

        if (serviceId != null)
        {
            // TODO: Implement check for used serviceids
            _context.Serviceids.Remove(serviceId);
            await _context.SaveChangesAsync();
        }
        else
        {
            throw new ResourceNotFoundError(
                "You need to specify either a ID or name of a serviceid.");
        }
    }

    /// <summary>
    /// Get serviceids filtered by name and/or id
    /// </summary>
    /// <param name="name">Filter by name</param>
    /// <param name="id">Filter by id</param>
    /// <returns>List of matching serviceids</returns>
    public async Task<List<Serviceid>> GetServiceIdsAsync(string? name = null, int? id = null)
    {
        var query = _context.Serviceids.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(s => s.Name == name);
        }

        if (id.HasValue)
        {
            query = query.Where(s => s.Id == id.Value);
        }

        return await query.ToListAsync();
    }
}
