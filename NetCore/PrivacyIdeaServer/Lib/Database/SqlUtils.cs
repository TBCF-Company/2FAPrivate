//  2018-10-02 Friedrich Weber <friedrich.weber@netknights.it>
//             Add chunked deletions
//  Converted to C# .NET Core 8 - 2026-02-05
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
using System.Linq.Expressions;

namespace PrivacyIdeaServer.Lib.Database;

/// <summary>
/// SQL utility functions for database operations
/// </summary>
public static class SqlUtils
{
    /// <summary>
    /// Delete all rows matching a given filter criterion from a table,
    /// using chunked deletes if chunksize is specified.
    /// 
    /// Deleting a large number of rows via just one SQL statement
    /// may cause deadlocks in a replicated setup. This method can
    /// be used to split the one big DELETE statement into multiple
    /// smaller statements ("chunks") which reduces the probability
    /// of deadlocks.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="context">Database context</param>
    /// <param name="filter">Filter expression to select rows to delete</param>
    /// <param name="chunkSize">Number of rows to delete in one chunk. If null, delete all at once.</param>
    /// <returns>Total number of deleted rows</returns>
    public static async Task<int> DeleteMatchingRowsAsync<T>(
        PrivacyIDEAContext context,
        Expression<Func<T, bool>> filter,
        int? chunkSize = null) where T : class
    {
        if (chunkSize == null || chunkSize <= 0)
        {
            // Delete all matching rows in one operation
            return await context.Set<T>()
                .Where(filter)
                .ExecuteDeleteAsync();
        }
        else
        {
            // Delete in chunks
            return await DeleteChunkedAsync(context, filter, chunkSize.Value);
        }
    }

    /// <summary>
    /// Delete all rows matching a given filter criterion from a table,
    /// but only delete *limit* rows at a time. Commit after each DELETE.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="context">Database context</param>
    /// <param name="filter">Filter expression to select rows to delete</param>
    /// <param name="limit">Number of rows to delete in one chunk (default: 1000)</param>
    /// <returns>Total number of deleted rows</returns>
    private static async Task<int> DeleteChunkedAsync<T>(
        PrivacyIDEAContext context,
        Expression<Func<T, bool>> filter,
        int limit = 1000) where T : class
    {
        if (limit <= 0)
        {
            throw new ArgumentException("Limit must be positive", nameof(limit));
        }

        int totalDeleted = 0;

        while (true)
        {
            // Delete up to 'limit' rows
            int deleted = await context.Set<T>()
                .Where(filter)
                .Take(limit)
                .ExecuteDeleteAsync();

            totalDeleted += deleted;

            // If we deleted fewer rows than the limit, we're done
            if (deleted < limit)
            {
                break;
            }
        }

        return totalDeleted;
    }

    /// <summary>
    /// Execute a raw SQL statement with parameters
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="sql">SQL statement</param>
    /// <param name="parameters">SQL parameters</param>
    /// <returns>Number of affected rows</returns>
    public static async Task<int> ExecuteRawSqlAsync(
        PrivacyIDEAContext context,
        string sql,
        params object[] parameters)
    {
        return await context.Database.ExecuteSqlRawAsync(sql, parameters);
    }
}
