// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace XmlSigningExample.Api.Services;

/// <summary>
/// Background service to periodically clean up expired signing sessions
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly IXmlSigningService _signingService;
    
    public SessionCleanupService(
        ILogger<SessionCleanupService> logger,
        IXmlSigningService signingService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _signingService = signingService ?? throw new ArgumentNullException(nameof(signingService));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup service starting");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run cleanup every minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                
                if (_signingService is XmlSigningService service)
                {
                    service.CleanupExpiredSessions();
                }
            }
            catch (OperationCanceledException)
            {
                // This is expected when the application is shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }
        }
        
        _logger.LogInformation("Session cleanup service stopping");
    }
}
