// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/audit.py
//
// (c) NetKnights GmbH 2024, https://netknights.it
// privacyIDEA is a fork of LinOTP
// May 08, 2014 Cornelius Kölbel
//
// 2016-11-29 Cornelius Kölbel <cornelius.koelbel@netknights.it>
//            Add timelimit, to only display recent entries
// 2014-10-17 Fix the empty result problem
//            Cornelius Kölbel, <cornelius@privacyidea.org>
//
// Copyright (C) 2010 - 2014 LSE Leading Security Experts GmbH
//
// This code is free software; you can redistribute it and/or
// modify it under the terms of the GNU AFFERO GENERAL PUBLIC LICENSE
// License as published by the Free Software Foundation; either
// version 3 of the License, or any later version.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PrivacyIdeaServer.Lib.Audit
{
    /// <summary>
    /// Pagination information for audit log queries
    /// </summary>
    public class AuditPagination
    {
        public List<Dictionary<string, object?>> AuditData { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int? Prev { get; set; }
        public int? Next { get; set; }
    }

    /// <summary>
    /// Search result for audit log queries
    /// </summary>
    public class AuditSearchResult
    {
        public List<Dictionary<string, object?>> AuditData { get; set; } = new();
        public List<string> AuditColumns { get; set; } = new();
        public int? Prev { get; set; }
        public int? Next { get; set; }
        public int Current { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Base interface for audit implementations
    /// </summary>
    public interface IAudit
    {
        /// <summary>
        /// Available audit log columns
        /// </summary>
        List<string> AvailableAuditColumns { get; }

        /// <summary>
        /// Logs audit data
        /// </summary>
        Task LogAsync(Dictionary<string, object?> data);

        /// <summary>
        /// Searches audit log entries
        /// </summary>
        Task<AuditPagination> SearchAsync(
            Dictionary<string, object?>? parameters = null,
            Dictionary<string, object?>? adminParams = null,
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 15,
            TimeSpan? timeLimit = null);

        /// <summary>
        /// Finalizes and signs the audit log entry
        /// </summary>
        Task FinalizeLogAsync();

        /// <summary>
        /// Gets the current audit data dictionary
        /// </summary>
        Dictionary<string, object?> GetAuditData();

        /// <summary>
        /// Clears the current audit data
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Base abstract class for audit implementations
    /// Provides common functionality for audit logging
    /// </summary>
    public abstract class AuditBase : IAudit
    {
        protected readonly ILogger _logger;
        protected readonly IConfiguration _config;
        protected readonly DateTime? _startDate;
        protected readonly Dictionary<string, object?> _auditData;

        protected AuditBase(
            ILogger logger,
            IConfiguration config,
            DateTime? startDate = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _startDate = startDate ?? DateTime.UtcNow;
            _auditData = new Dictionary<string, object?>();
        }

        /// <summary>
        /// Available columns in the audit log
        /// </summary>
        public abstract List<string> AvailableAuditColumns { get; }

        /// <summary>
        /// Logs audit data to the underlying storage
        /// </summary>
        public virtual async Task LogAsync(Dictionary<string, object?> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Merge data into audit dictionary
            foreach (var kvp in data)
            {
                _auditData[kvp.Key] = kvp.Value;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Searches audit log entries with pagination and filtering
        /// </summary>
        public abstract Task<AuditPagination> SearchAsync(
            Dictionary<string, object?>? parameters = null,
            Dictionary<string, object?>? adminParams = null,
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 15,
            TimeSpan? timeLimit = null);

        /// <summary>
        /// Finalizes the audit log entry with signature and persists it
        /// </summary>
        public abstract Task FinalizeLogAsync();

        /// <summary>
        /// Gets the current audit data
        /// </summary>
        public virtual Dictionary<string, object?> GetAuditData()
        {
            return new Dictionary<string, object?>(_auditData);
        }

        /// <summary>
        /// Clears the current audit data
        /// </summary>
        public virtual void Clear()
        {
            _auditData.Clear();
        }

        /// <summary>
        /// Creates a signature for audit data
        /// </summary>
        protected virtual string CreateSignature(Dictionary<string, object?> data)
        {
            // TODO: Implement proper cryptographic signature
            // This should use HSM or key management service
            // For now, return a placeholder
            return "UNSIGNED";
        }

        /// <summary>
        /// Validates the current audit data
        /// </summary>
        protected virtual bool ValidateAuditData()
        {
            // Ensure required fields are present
            // TODO: Define required fields based on audit policy
            return true;
        }
    }

    /// <summary>
    /// Factory for creating audit instances
    /// Equivalent to Python's getAudit function
    /// </summary>
    public class AuditFactory
    {
        private readonly ILogger<AuditFactory> _logger;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _serviceProvider;

        public AuditFactory(
            ILogger<AuditFactory> logger,
            IConfiguration config,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Creates an audit instance based on configuration
        /// </summary>
        /// <param name="startDate">Start date for the audit session</param>
        /// <returns>Configured audit instance</returns>
        public async Task<IAudit> CreateAuditAsync(DateTime? startDate = null)
        {
            _logger.LogDebug("Creating audit instance");

            var auditModule = _config["PI_AUDIT_MODULE"] 
                ?? "PrivacyIdeaServer.Lib.Audit.SqlAudit";

            try
            {
                // Load audit module type
                var type = GetAuditModuleType(auditModule);
                
                if (type == null)
                {
                    _logger.LogWarning(
                        "Audit module {Module} not found, using default", 
                        auditModule);
                    // Fall back to default implementation
                    type = typeof(DefaultAudit);
                }

                // Create instance with dependency injection
                var audit = (IAudit?)Activator.CreateInstance(
                    type,
                    _serviceProvider.GetService(typeof(ILogger<>).MakeGenericType(type)),
                    _config,
                    startDate);

                if (audit == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to create audit instance for {auditModule}");
                }

                _logger.LogDebug("Created audit instance of type {Type}", type.Name);
                return await Task.FromResult(audit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit instance for {Module}", auditModule);
                throw;
            }
        }

        /// <summary>
        /// Resolves audit module type from string
        /// </summary>
        private Type? GetAuditModuleType(string moduleName)
        {
            try
            {
                // Try to load type from current assembly first
                var assembly = Assembly.GetExecutingAssembly();
                var type = assembly.GetType(moduleName);

                if (type != null)
                    return type;

                // Try to load from all loaded assemblies
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(moduleName);
                    if (type != null)
                        return type;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading audit module type {Module}", moduleName);
                return null;
            }
        }
    }

    /// <summary>
    /// Default in-memory audit implementation
    /// Used as fallback when no specific audit module is configured
    /// </summary>
    public class DefaultAudit : AuditBase
    {
        private static readonly List<Dictionary<string, object?>> _auditLog = new();
        private static readonly object _lock = new();

        public DefaultAudit(
            ILogger<DefaultAudit> logger,
            IConfiguration config,
            DateTime? startDate = null)
            : base(logger, config, startDate)
        {
        }

        public override List<string> AvailableAuditColumns => new()
        {
            "id", "date", "signature", "action", "success", "serial",
            "token_type", "user", "realm", "resolver", "administrator",
            "action_detail", "info", "privacyidea_server", "client",
            "log_level", "clearance"
        };

        public override async Task<AuditPagination> SearchAsync(
            Dictionary<string, object?>? parameters = null,
            Dictionary<string, object?>? adminParams = null,
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 15,
            TimeSpan? timeLimit = null)
        {
            await Task.CompletedTask;

            lock (_lock)
            {
                var filtered = _auditLog.AsEnumerable();

                // Apply filters
                if (parameters != null)
                {
                    filtered = ApplyFilters(filtered, parameters);
                }

                // Apply time limit
                if (timeLimit.HasValue)
                {
                    var cutoff = DateTime.UtcNow - timeLimit.Value;
                    filtered = filtered.Where(entry =>
                    {
                        if (entry.TryGetValue("date", out var dateObj) && dateObj is DateTime date)
                        {
                            return date >= cutoff;
                        }
                        return true;
                    });
                }

                // Sort
                filtered = sortOrder.ToLowerInvariant() == "asc"
                    ? filtered.OrderBy(e => e.GetValueOrDefault("date"))
                    : filtered.OrderByDescending(e => e.GetValueOrDefault("date"));

                var total = filtered.Count();
                var skip = (page - 1) * pageSize;
                var data = filtered.Skip(skip).Take(pageSize).ToList();

                return new AuditPagination
                {
                    AuditData = data,
                    Page = page,
                    PageSize = pageSize,
                    Total = total,
                    Prev = page > 1 ? page - 1 : null,
                    Next = skip + pageSize < total ? page + 1 : null
                };
            }
        }

        public override async Task FinalizeLogAsync()
        {
            if (!ValidateAuditData())
            {
                _logger.LogWarning("Audit data validation failed");
                return;
            }

            // Add signature
            _auditData["signature"] = CreateSignature(_auditData);
            _auditData["date"] = _startDate;

            lock (_lock)
            {
                _auditLog.Add(new Dictionary<string, object?>(_auditData));
            }

            _logger.LogDebug("Finalized audit log entry");
            await Task.CompletedTask;
        }

        private IEnumerable<Dictionary<string, object?>> ApplyFilters(
            IEnumerable<Dictionary<string, object?>> data,
            Dictionary<string, object?> filters)
        {
            foreach (var filter in filters)
            {
                data = data.Where(entry =>
                {
                    if (entry.TryGetValue(filter.Key, out var value))
                    {
                        return value?.ToString()?.Contains(filter.Value?.ToString() ?? "", 
                            StringComparison.OrdinalIgnoreCase) ?? false;
                    }
                    return false;
                });
            }
            return data;
        }
    }

    /// <summary>
    /// Service for searching audit logs
    /// Equivalent to Python's search function
    /// </summary>
    public class AuditSearchService
    {
        private readonly ILogger<AuditSearchService> _logger;
        private readonly IConfiguration _config;
        private readonly AuditFactory _auditFactory;

        public AuditSearchService(
            ILogger<AuditSearchService> logger,
            IConfiguration config,
            AuditFactory auditFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _auditFactory = auditFactory ?? throw new ArgumentNullException(nameof(auditFactory));
        }

        /// <summary>
        /// Searches audit log with pagination and filtering
        /// </summary>
        /// <param name="parameters">Search parameters</param>
        /// <param name="adminParams">Admin-specific parameters</param>
        /// <returns>Audit search result</returns>
        public async Task<AuditSearchResult> SearchAsync(
            Dictionary<string, object?>? parameters = null,
            Dictionary<string, object?>? adminParams = null)
        {
            _logger.LogDebug("Searching audit log");

            var audit = await _auditFactory.CreateAuditAsync();
            parameters ??= new Dictionary<string, object?>();

            // Extract special parameters
            var sortOrder = ExtractParameter<string>(parameters, "sortorder", "desc");
            var page = Math.Max(1, ExtractParameter<int>(parameters, "page", 1));
            var pageSize = ExtractParameter<int>(parameters, "page_size", 15);
            
            if (pageSize < 1)
            {
                _logger.LogInformation(
                    "Requested Audit-Log page size is negative. Setting it to 15");
                pageSize = 15;
            }

            var timeLimitStr = ExtractParameter<string?>(parameters, "timelimit", null);
            var timeLimit = ParseTimeLimit(timeLimitStr);

            var hiddenColumns = ExtractParameter<List<string>>(
                parameters, "hidden_columns", new List<string>());

            // Perform search
            var pagination = await audit.SearchAsync(
                parameters,
                adminParams,
                sortOrder,
                page,
                pageSize,
                timeLimit);

            // Remove hidden columns from results
            if (hiddenColumns.Any())
            {
                for (int i = 0; i < pagination.AuditData.Count; i++)
                {
                    pagination.AuditData[i] = pagination.AuditData[i]
                        .Where(kvp => !hiddenColumns.Contains(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
            }

            var visibleColumns = audit.AvailableAuditColumns
                .Where(col => !hiddenColumns.Contains(col))
                .ToList();

            return new AuditSearchResult
            {
                AuditData = pagination.AuditData,
                AuditColumns = visibleColumns,
                Prev = pagination.Prev,
                Next = pagination.Next,
                Current = pagination.Page,
                Count = pagination.Total
            };
        }

        private T ExtractParameter<T>(
            Dictionary<string, object?> parameters,
            string key,
            T defaultValue)
        {
            if (parameters.TryGetValue(key, out var value))
            {
                parameters.Remove(key);
                
                if (value is T typedValue)
                    return typedValue;
                
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T))!;
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        private TimeSpan? ParseTimeLimit(string? timeLimitStr)
        {
            if (string.IsNullOrWhiteSpace(timeLimitStr))
                return null;

            // Parse formats like "1d", "2h", "30m", "60s"
            try
            {
                var value = timeLimitStr.TrimEnd('d', 'h', 'm', 's', 'D', 'H', 'M', 'S');
                var unit = timeLimitStr[^1..].ToLowerInvariant();

                if (!int.TryParse(value, out int num))
                    return null;

                return unit switch
                {
                    "d" => TimeSpan.FromDays(num),
                    "h" => TimeSpan.FromHours(num),
                    "m" => TimeSpan.FromMinutes(num),
                    "s" => TimeSpan.FromSeconds(num),
                    _ => TimeSpan.FromSeconds(num)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing time limit: {TimeLimit}", timeLimitStr);
                return null;
            }
        }
    }
}
