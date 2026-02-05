// SPDX-FileCopyrightText: (C) 2021 Paul Lettich <paul.lettich@netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Info: http://www.privacyidea.org
//
// This code is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// This module provides the functionality to register export or import functions
/// for separate parts of the privacyIDEA server configuration.
/// </summary>
public static class ExportRegistry
{
    private static readonly Dictionary<string, Func<Dictionary<string, object>>> ExportFunctions = new();
    private static readonly Dictionary<string, ImportFunction> ImportFunctions = new();

    /// <summary>
    /// Register an export function with a given name.
    /// </summary>
    /// <param name="name">The name with which the function will be registered</param>
    /// <param name="exportFunc">The function that exports configuration data</param>
    public static void RegisterExport(string name, Func<Dictionary<string, object>> exportFunc)
    {
        if (ExportFunctions.ContainsKey(name))
        {
            Console.Error.WriteLine($"Warning: Exporter function with name '{name}' already exists! Overwriting.");
        }
        ExportFunctions[name] = exportFunc;
    }

    /// <summary>
    /// Register an import function with a given name and priority.
    /// </summary>
    /// <param name="name">The name with which the function will be registered</param>
    /// <param name="importFunc">The function that imports configuration data</param>
    /// <param name="priority">The priority of the importer function (default is 99, lower comes first)</param>
    public static void RegisterImport(string name, Action<Dictionary<string, object>> importFunc, int priority = 99)
    {
        if (ImportFunctions.ContainsKey(name))
        {
            Console.Error.WriteLine($"Warning: Importer function with name '{name}' already exists! Overwriting.");
        }
        ImportFunctions[name] = new ImportFunction(importFunc, priority);
    }

    /// <summary>
    /// Get all registered export functions.
    /// </summary>
    public static IReadOnlyDictionary<string, Func<Dictionary<string, object>>> GetExportFunctions()
        => ExportFunctions;

    /// <summary>
    /// Get all registered import functions, sorted by priority.
    /// </summary>
    public static IEnumerable<(string Name, Action<Dictionary<string, object>> Function, int Priority)> GetImportFunctions()
        => ImportFunctions
            .OrderBy(kvp => kvp.Value.Priority)
            .Select(kvp => (kvp.Key, kvp.Value.Function, kvp.Value.Priority));

    private record ImportFunction(Action<Dictionary<string, object>> Function, int Priority);
}
