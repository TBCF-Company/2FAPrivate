// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software; you can redistribute it and/or
// modify it under the terms of the GNU AFFERO GENERAL PUBLIC LICENSE
// as published by the Free Software Foundation; either
// version 3 of the License, or any later version.
//
// Converted from Python to C# from privacyidea/models/utils.py

using System;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// Utility methods for datetime operations
    /// Equivalent to Python's utc_now() function
    /// </summary>
    public static class DateTimeUtils
    {
        /// <summary>
        /// Return the current UTC time as a DateTime object.
        /// Equivalent to Python: datetime.now(timezone.utc).replace(tzinfo=None)
        /// </summary>
        public static DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Interface for entities with save/delete methods
    /// Equivalent to Python's MethodsMixin class
    /// </summary>
    public interface IMethodsMixin
    {
        int? Id { get; }
    }

    /// <summary>
    /// Extension methods for entities implementing IMethodsMixin
    /// Provides common database operations
    /// </summary>
    public static class MethodsMixinExtensions
    {
        /// <summary>
        /// Save the entity to the database
        /// Returns the entity ID if it has one
        /// </summary>
        public static async Task<int?> SaveAsync<T>(this T entity, PrivacyIdeaContext context) 
            where T : class, IMethodsMixin
        {
            context.Set<T>().Update(entity);
            await context.SaveChangesAsync();
            return entity.Id;
        }

        /// <summary>
        /// Delete the entity from the database
        /// Returns the entity ID if it had one
        /// </summary>
        public static async Task<int?> DeleteAsync<T>(this T entity, PrivacyIdeaContext context) 
            where T : class, IMethodsMixin
        {
            var id = entity.Id;
            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
            return id;
        }
    }
}
