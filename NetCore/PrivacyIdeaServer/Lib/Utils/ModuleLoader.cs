// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Dynamic module and class loading utilities.
/// </summary>
public static class ModuleLoader
{
    /// <summary>
    /// Load a class from a given package/namespace dynamically.
    /// </summary>
    /// <param name="packageName">Full namespace path (e.g., "PrivacyIdeaServer.Lib.AuditModules")</param>
    /// <param name="className">Name of the class to load</param>
    /// <param name="checkMethod">Optional method name to verify the class has</param>
    /// <returns>The loaded Type</returns>
    /// <exception cref="TypeLoadException">If type cannot be loaded</exception>
    /// <exception cref="ArgumentException">If required method is missing</exception>
    public static Type GetModuleClass(string packageName, string className, string? checkMethod = null)
    {
        var fullTypeName = $"{packageName}.{className}";
        
        // Try to load from all loaded assemblies
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == fullTypeName);

        if (type == null)
        {
            throw new TypeLoadException($"{packageName} has no class {className}");
        }

        if (!string.IsNullOrWhiteSpace(checkMethod))
        {
            var method = type.GetMethod(checkMethod);
            if (method == null)
            {
                throw new ArgumentException(
                    $"Class {fullTypeName} has no method '{checkMethod}'");
            }
        }

        return type;
    }
}
