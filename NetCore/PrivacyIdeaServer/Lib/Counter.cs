//  2018-26-09 Paul Lettich <paul.lettich@netknights.it>
//             Add decrease/reset functions
//  2018-03-01 Cornelius Kölbel <cornelius.koelbel@netknights.it>
//  Converted to C# .NET Core 8 - 2026-02-05
//
//  Copyright (C) 2018 Cornelius Kölbel
//  License:  AGPLv3
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
/// This module is used to modify counters in the database
/// </summary>
public class Counter
{
    private readonly PrivacyIDEAContext _context;
    private readonly string _nodeName;

    public Counter(PrivacyIDEAContext context, string? nodeName = null)
    {
        _context = context;
        _nodeName = nodeName ?? Environment.MachineName;
    }

    /// <summary>
    /// Increase the counter value in the database.
    /// If the counter does not exist yet, create the counter.
    /// </summary>
    /// <param name="counterName">The name/identifier of the counter</param>
    public async Task IncreaseAsync(string counterName)
    {
        var counter = await _context.EventCounters
            .FirstOrDefaultAsync(c => c.Counter == counterName && c.Node == _nodeName);

        if (counter == null)
        {
            counter = new EventCounter
            {
                Counter = counterName,
                CounterValue = 0,
                Node = _nodeName
            };
            _context.EventCounters.Add(counter);
        }

        counter.CounterValue++;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Decrease the counter value in the database.
    /// If the counter does not exist yet, create the counter.
    /// Also checks whether the counter is allowed to become negative.
    /// </summary>
    /// <param name="counterName">The name/identifier of the counter</param>
    /// <param name="allowNegative">Whether the counter can become negative. 
    /// Note that even if this flag is not set, the counter may become negative due to concurrent queries.</param>
    public async Task DecreaseAsync(string counterName, bool allowNegative = false)
    {
        var counter = await _context.EventCounters
            .FirstOrDefaultAsync(c => c.Counter == counterName && c.Node == _nodeName);

        if (counter == null)
        {
            counter = new EventCounter
            {
                Counter = counterName,
                CounterValue = 0,
                Node = _nodeName
            };
            _context.EventCounters.Add(counter);
        }

        // We are allowed to decrease the current counter object only if the overall
        // counter value is positive (because individual rows may be negative then),
        // or if we allow negative values. Otherwise, we need to reset all rows of all nodes.
        var currentValue = await ReadAsync(counterName);
        if (currentValue > 0 || allowNegative)
        {
            counter.CounterValue--;
        }
        else
        {
            await ResetCounterOnAllNodesAsync(counterName);
            return;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Reset the counter value in the database to 0.
    /// If the counter does not exist yet, create the counter.
    /// </summary>
    /// <param name="counterName">The name/identifier of the counter</param>
    public async Task ResetAsync(string counterName)
    {
        var counterExists = await _context.EventCounters
            .AnyAsync(c => c.Counter == counterName);

        if (!counterExists)
        {
            var counter = new EventCounter
            {
                Counter = counterName,
                CounterValue = 0,
                Node = _nodeName
            };
            _context.EventCounters.Add(counter);
            await _context.SaveChangesAsync();
        }
        else
        {
            await ResetCounterOnAllNodesAsync(counterName);
        }
    }

    /// <summary>
    /// Read the counter value from the database.
    /// If the counter_name does not exist, 0 is returned.
    /// </summary>
    /// <param name="counterName">The name of the counter</param>
    /// <returns>The value of the counter</returns>
    public async Task<long> ReadAsync(string counterName)
    {
        var sum = await _context.EventCounters
            .Where(c => c.Counter == counterName)
            .SumAsync(c => (long?)c.CounterValue);

        return sum ?? 0;
    }

    /// <summary>
    /// Reset all EventCounter rows that set a value for counterName to zero,
    /// regardless of the node column.
    /// </summary>
    /// <param name="counterName">The name/identifier of the counter</param>
    private async Task ResetCounterOnAllNodesAsync(string counterName)
    {
        await _context.EventCounters
            .Where(c => c.Counter == counterName)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.CounterValue, 0));
    }
}
