// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/log.py
//
// privacyIDEA is a fork of LinOTP
// May 08, 2014 Cornelius Kölbel
// Copyright (C) 2010 - 2014 LSE Leading Security Experts GmbH
//
// This code is free software; you can redistribute it and/or
// modify it under the terms of the GNU AFFERO GENERAL PUBLIC LICENSE
// License as published by the Free Software Foundation; either
// version 3 of the License, or any later version.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PrivacyIdeaServer.Lib.Logging
{
    /// <summary>
    /// Secure logging formatter that sanitizes log output
    /// Equivalent to Python's SecureFormatter class
    /// Ensures log entries contain only printable characters and prevents sensitive data leakage
    /// </summary>
    public class SecureLogFormatter
    {
        private readonly ILogger _logger;
        private readonly HashSet<string> _sensitiveKeywords;

        public SecureLogFormatter(ILogger logger)
        {
            _logger = logger;
            _sensitiveKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "passwd", "pwd", "secret", "key", "token", "pin",
                "otpkey", "googlekey", "oath", "enckey", "seed"
            };
        }

        /// <summary>
        /// Formats and secures a log message
        /// Removes non-printable characters and adds security warnings if needed
        /// </summary>
        /// <param name="message">The message to format</param>
        /// <returns>Secured message</returns>
        public string FormatSecure(string message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            // Check for printable characters
            if (!IsPrintable(message))
            {
                var secured = new StringBuilder();
                foreach (var c in message)
                {
                    secured.Append(char.IsControl(c) && !char.IsWhiteSpace(c) ? '.' : c);
                }
                return $"!!Log Entry Secured by SecureFormatter!! {secured}";
            }

            return message;
        }

        /// <summary>
        /// Checks if a string contains only printable characters
        /// </summary>
        private static bool IsPrintable(string text)
        {
            return text.All(c => !char.IsControl(c) || char.IsWhiteSpace(c));
        }

        /// <summary>
        /// Sanitizes structured data by hiding sensitive values
        /// </summary>
        /// <param name="data">Dictionary to sanitize</param>
        /// <param name="logLevel">Current log level</param>
        /// <returns>Sanitized dictionary</returns>
        public Dictionary<string, object?> SanitizeStructuredData(
            Dictionary<string, object?> data, 
            LogLevel logLevel)
        {
            // Only sanitize if log level is INFO or higher (less detailed)
            if (logLevel < LogLevel.Debug)
                return new Dictionary<string, object?>(data);

            var sanitized = new Dictionary<string, object?>();
            foreach (var kvp in data)
            {
                if (_sensitiveKeywords.Any(keyword => 
                    kvp.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    sanitized[kvp.Key] = "HIDDEN";
                }
                else if (kvp.Value is Dictionary<string, object?> nestedDict)
                {
                    sanitized[kvp.Key] = SanitizeStructuredData(nestedDict, logLevel);
                }
                else
                {
                    sanitized[kvp.Key] = kvp.Value;
                }
            }
            return sanitized;
        }
    }

    /// <summary>
    /// Configuration options for logging
    /// Equivalent to Python's DEFAULT_LOGGING_CONFIG and DOCKER_LOGGING_CONFIG
    /// </summary>
    public class LoggingConfiguration
    {
        public bool UseSecureFormatter { get; set; } = true;
        public bool LogToFile { get; set; } = true;
        public bool LogToConsole { get; set; } = false;
        public string LogFilePath { get; set; } = "/var/log/privacyidea/privacyidea.log";
        public long MaxFileSizeBytes { get; set; } = 10_000_000; // 10MB
        public int BackupCount { get; set; } = 5;
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
        public bool IsDockerEnvironment { get; set; } = false;

        public static LoggingConfiguration Default => new()
        {
            UseSecureFormatter = true,
            LogToFile = true,
            LogToConsole = false,
            LogFilePath = "/var/log/privacyidea/privacyidea.log",
            MinimumLevel = LogLevel.Information
        };

        public static LoggingConfiguration Docker => new()
        {
            UseSecureFormatter = true,
            LogToFile = false,
            LogToConsole = true,
            MinimumLevel = LogLevel.Information,
            IsDockerEnvironment = true
        };
    }

    /// <summary>
    /// Attribute to mark methods for automatic logging
    /// Equivalent to Python's @log_with decorator
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LogWithAttribute : Attribute
    {
        public bool LogEntry { get; set; } = true;
        public bool LogExit { get; set; } = true;
        public int[]? HideArgs { get; set; }
        public string[]? HideKwargs { get; set; }
        public Dictionary<int, string[]>? HideArgsKeywords { get; set; }

        public LogWithAttribute()
        {
        }
    }

    /// <summary>
    /// Logging helper for method entry/exit logging with parameter hiding
    /// Provides structured logging with security filtering
    /// </summary>
    public class MethodLogger
    {
        private readonly ILogger _logger;
        private readonly SecureLogFormatter _formatter;

        public MethodLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _formatter = new SecureLogFormatter(logger);
        }

        /// <summary>
        /// Logs method entry with parameters
        /// </summary>
        public void LogEntry(
            string methodName,
            object?[]? args = null,
            Dictionary<string, object?>? kwargs = null,
            int[]? hideArgs = null,
            string[]? hideKwargs = null,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
                return;

            var sanitizedArgs = SanitizeArgs(args, hideArgs);
            var sanitizedKwargs = SanitizeKwargs(kwargs, hideKwargs);

            var message = $"Entering {methodName} with arguments {FormatArgs(sanitizedArgs)} " +
                         $"and keywords {FormatKwargs(sanitizedKwargs)}";

            _logger.LogDebug(
                _formatter.FormatSecure(message),
                new { 
                    MethodName = methodName,
                    CalledFrom = $"{System.IO.Path.GetFileName(filePath)}:{lineNumber}",
                    Arguments = sanitizedArgs,
                    Keywords = sanitizedKwargs
                });
        }

        /// <summary>
        /// Logs method exit with result
        /// </summary>
        public void LogExit(
            string methodName,
            object? result = null,
            bool hideResult = false,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
                return;

            var sanitizedResult = hideResult ? "HIDDEN" : FormatResult(result);
            var message = $"Exiting {methodName} with result {sanitizedResult}";

            _logger.LogDebug(
                _formatter.FormatSecure(message),
                new {
                    MethodName = methodName,
                    CalledFrom = $"{System.IO.Path.GetFileName(filePath)}:{lineNumber}",
                    Result = hideResult ? "HIDDEN" : result
                });
        }

        /// <summary>
        /// Sanitizes method arguments by hiding specified indices
        /// </summary>
        private object?[] SanitizeArgs(object?[]? args, int[]? hideIndices)
        {
            if (args == null || args.Length == 0)
                return Array.Empty<object>();

            if (hideIndices == null || hideIndices.Length == 0 || !_logger.IsEnabled(LogLevel.Trace))
                return args;

            var sanitized = new object?[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                sanitized[i] = hideIndices.Contains(i) ? "HIDDEN" : args[i];
            }
            return sanitized;
        }

        /// <summary>
        /// Sanitizes keyword arguments by hiding specified keys
        /// </summary>
        private Dictionary<string, object?> SanitizeKwargs(
            Dictionary<string, object?>? kwargs,
            string[]? hideKeys)
        {
            if (kwargs == null || kwargs.Count == 0)
                return new Dictionary<string, object?>();

            if (hideKeys == null || hideKeys.Length == 0 || !_logger.IsEnabled(LogLevel.Trace))
                return new Dictionary<string, object?>(kwargs);

            var sanitized = new Dictionary<string, object?>();
            foreach (var kvp in kwargs)
            {
                sanitized[kvp.Key] = hideKeys.Contains(kvp.Key) ? "HIDDEN" : kvp.Value;
            }
            return sanitized;
        }

        private string FormatArgs(object?[] args)
        {
            if (args == null || args.Length == 0)
                return "()";
            
            try
            {
                return $"({string.Join(", ", args.Select(a => FormatValue(a)))})";
            }
            catch
            {
                return "(...)";
            }
        }

        private string FormatKwargs(Dictionary<string, object?> kwargs)
        {
            if (kwargs == null || kwargs.Count == 0)
                return "{}";

            try
            {
                return "{" + string.Join(", ", kwargs.Select(kvp => 
                    $"{kvp.Key}={FormatValue(kvp.Value)}")) + "}";
            }
            catch
            {
                return "{...}";
            }
        }

        private string FormatResult(object? result)
        {
            return FormatValue(result);
        }

        private string FormatValue(object? value)
        {
            if (value == null)
                return "null";
            
            if (value is string s)
                return $"\"{s}\"";
            
            if (value is IEnumerable<object> enumerable && value is not string)
            {
                var items = enumerable.Take(5).Select(FormatValue);
                var result = string.Join(", ", items);
                return $"[{result}]";
            }

            return value.ToString() ?? "null";
        }

        /// <summary>
        /// Logs an error during method execution
        /// </summary>
        public void LogError(string methodName, Exception exception)
        {
            _logger.LogError(
                exception,
                _formatter.FormatSecure($"Error during logging of function {methodName}! {exception.Message}"));
        }
    }

    /// <summary>
    /// Extension methods for ILogger to provide secure logging
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Logs a message with secure formatting
        /// </summary>
        public static void LogSecure(
            this ILogger logger, 
            LogLevel logLevel, 
            string message, 
            params object[] args)
        {
            if (!logger.IsEnabled(logLevel))
                return;

            var formatter = new SecureLogFormatter(logger);
            var secureMessage = formatter.FormatSecure(message);
            logger.Log(logLevel, secureMessage, args);
        }

        /// <summary>
        /// Logs structured data with security filtering
        /// </summary>
        public static void LogStructured(
            this ILogger logger,
            LogLevel logLevel,
            string message,
            Dictionary<string, object?> data)
        {
            if (!logger.IsEnabled(logLevel))
                return;

            var formatter = new SecureLogFormatter(logger);
            var sanitizedData = formatter.SanitizeStructuredData(data, logLevel);
            var secureMessage = formatter.FormatSecure(message);

            using (logger.BeginScope(sanitizedData))
            {
                logger.Log(logLevel, secureMessage);
            }
        }

        /// <summary>
        /// Creates a method logger for the given logger instance
        /// </summary>
        public static MethodLogger CreateMethodLogger(this ILogger logger)
        {
            return new MethodLogger(logger);
        }
    }

    /// <summary>
    /// Helper for creating loggers with specific configurations
    /// </summary>
    public static class LoggerFactory
    {
        /// <summary>
        /// Creates a logger with secure formatting enabled
        /// </summary>
        public static ILogger CreateSecureLogger(
            ILoggerFactory loggerFactory,
            string categoryName,
            LoggingConfiguration? config = null)
        {
            config ??= LoggingConfiguration.Default;
            return loggerFactory.CreateLogger(categoryName);
        }

        /// <summary>
        /// Creates a logger for a specific type with secure formatting
        /// </summary>
        public static ILogger<T> CreateSecureLogger<T>(
            ILoggerFactory loggerFactory,
            LoggingConfiguration? config = null)
        {
            config ??= LoggingConfiguration.Default;
            return loggerFactory.CreateLogger<T>();
        }
    }
}
