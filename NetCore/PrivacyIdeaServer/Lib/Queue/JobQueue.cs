//  2019-02-04 Friedrich Weber <friedrich.weber@netknights.it>
//             Add a job queue
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

namespace PrivacyIdeaServer.Lib.Queue;

/// <summary>
/// Base interface for job queue implementations.
/// In .NET, consider using Hangfire, Quartz.NET, or Azure Queue Storage.
/// </summary>
public interface IJobQueue
{
    /// <summary>
    /// Register a job with the queue
    /// </summary>
    /// <param name="name">Unique name of the job</param>
    /// <param name="jobAction">The job action to execute</param>
    void RegisterJob(string name, Action<object[]?, Dictionary<string, object>?> jobAction);

    /// <summary>
    /// Enqueue a job for execution
    /// </summary>
    /// <param name="name">Name of the job to execute</param>
    /// <param name="args">Positional arguments</param>
    /// <param name="kwargs">Keyword arguments</param>
    void Enqueue(string name, object[]? args = null, Dictionary<string, object>? kwargs = null);
}

/// <summary>
/// Job collector for registering jobs before the app is fully initialized.
/// In .NET, this pattern is less common because dependency injection
/// handles most of the initialization concerns.
/// </summary>
public class JobCollector
{
    private readonly Dictionary<string, (Action<object[]?, Dictionary<string, object>?> Func, object[] Args, Dictionary<string, object> Kwargs)> _jobs = new();
    private readonly ILogger<JobCollector>? _logger;

    public JobCollector(ILogger<JobCollector>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all registered jobs
    /// </summary>
    public IReadOnlyDictionary<string, (Action<object[]?, Dictionary<string, object>?> Func, object[] Args, Dictionary<string, object> Kwargs)> Jobs => _jobs;

    /// <summary>
    /// Register a job with the collector.
    /// </summary>
    /// <param name="name">Unique name of the job</param>
    /// <param name="func">Function of the job</param>
    /// <param name="args">Arguments passed to the job queue's register_job method</param>
    /// <param name="kwargs">Keyword arguments passed to the job queue's register_job method</param>
    public void RegisterJob(string name, Action<object[]?, Dictionary<string, object>?> func, object[]? args = null, Dictionary<string, object>? kwargs = null)
    {
        if (_jobs.ContainsKey(name))
        {
            throw new InvalidOperationException($"Duplicate jobs: {name}");
        }

        _jobs[name] = (func, args ?? Array.Empty<object>(), kwargs ?? new Dictionary<string, object>());
    }

    /// <summary>
    /// Register all collected jobs with a job queue instance
    /// </summary>
    /// <param name="jobQueue">The job queue to register jobs with</param>
    public void RegisterWithQueue(IJobQueue jobQueue)
    {
        _logger?.LogInformation("Registering {Count} jobs with job queue", _jobs.Count);

        foreach (var (name, (func, args, kwargs)) in _jobs)
        {
            jobQueue.RegisterJob(name, func);
        }
    }
}

/// <summary>
/// Job queue manager for the application.
/// This provides a centralized way to manage background jobs.
/// 
/// In a real .NET application, you would typically use:
/// - Hangfire for background job processing
/// - Quartz.NET for scheduled tasks
/// - Azure Queue Storage / AWS SQS for distributed queuing
/// </summary>
public class JobQueueManager
{
    private static IJobQueue? _instance;
    private static readonly object _lock = new();

    /// <summary>
    /// Initialize the job queue with a specific implementation
    /// </summary>
    public static void Initialize(IJobQueue jobQueue)
    {
        lock (_lock)
        {
            if (_instance != null)
            {
                throw new InvalidOperationException("Job queue already initialized");
            }
            _instance = jobQueue;
        }
    }

    /// <summary>
    /// Check if a job queue is configured
    /// </summary>
    public static bool HasJobQueue()
    {
        return _instance != null;
    }

    /// <summary>
    /// Get the configured job queue instance
    /// </summary>
    /// <exception cref="ServerError">Thrown if no job queue is configured</exception>
    public static IJobQueue GetJobQueue()
    {
        if (_instance == null)
        {
            throw new ServerError("privacyIDEA has no job queue configured!");
        }
        return _instance;
    }

    /// <summary>
    /// Wrap a job and return a function that can be used like the original function.
    /// The returned function will enqueue the job and return the specified result.
    /// </summary>
    /// <typeparam name="TResult">Type of the result</typeparam>
    /// <param name="name">Name of the job</param>
    /// <param name="result">Result to return when the wrapped function is called</param>
    /// <returns>A function that enqueues the job</returns>
    public static Func<object[]?, Dictionary<string, object>?, TResult> WrapJob<TResult>(string name, TResult result)
    {
        return (args, kwargs) =>
        {
            GetJobQueue().Enqueue(name, args, kwargs);
            return result;
        };
    }
}

/// <summary>
/// Simple in-memory job queue implementation for testing/development.
/// For production, use Hangfire or another robust solution.
/// </summary>
public class InMemoryJobQueue : IJobQueue
{
    private readonly Dictionary<string, Action<object[]?, Dictionary<string, object>?>> _jobs = new();
    private readonly ILogger<InMemoryJobQueue> _logger;

    public InMemoryJobQueue(ILogger<InMemoryJobQueue> logger)
    {
        _logger = logger;
    }

    public void RegisterJob(string name, Action<object[]?, Dictionary<string, object>?> jobAction)
    {
        _jobs[name] = jobAction;
        _logger.LogInformation("Registered job: {JobName}", name);
    }

    public void Enqueue(string name, object[]? args = null, Dictionary<string, object>? kwargs = null)
    {
        if (!_jobs.TryGetValue(name, out var jobAction))
        {
            throw new InvalidOperationException($"Job not found: {name}");
        }

        _logger.LogInformation("Enqueueing job: {JobName}", name);

        // In a real implementation, this would be queued for async execution
        // For this simple version, we just execute it synchronously
        Task.Run(() =>
        {
            try
            {
                jobAction(args, kwargs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing job {JobName}", name);
            }
        });
    }
}
