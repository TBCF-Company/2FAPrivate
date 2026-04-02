using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// Audit Service implementation
/// Maps to Python: privacyidea/lib/audit.py
/// </summary>
public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IUnitOfWork unitOfWork,
        ICryptoService cryptoService,
        ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditRepository = unitOfWork.Audit;
        _cryptoService = cryptoService;
        _logger = logger;
    }

    public async Task LogAsync(string action, bool success, string? user = null, string? realm = null, 
                               string? serial = null, string? tokenType = null, string? info = null)
    {
        await LogAsync(new AuditLogRequest
        {
            Action = action,
            Success = success,
            User = user,
            Realm = realm,
            Serial = serial,
            TokenType = tokenType,
            Info = info
        });
    }

    public async Task LogAsync(AuditLogRequest request)
    {
        try
        {
            var entry = new AuditEntry
            {
                Date = DateTime.UtcNow,
                Action = request.Action,
                Success = request.Success,
                Serial = request.Serial,
                TokenType = request.TokenType,
                User = request.User,
                Realm = request.Realm,
                Resolver = request.Resolver,
                Administrator = request.Administrator,
                ActionDetail = request.ActionDetail,
                Info = request.Info,
                Client = request.Client,
                Policies = request.Policies,
                StartDate = request.StartDate,
                Duration = request.StartDate.HasValue 
                    ? (DateTime.UtcNow - request.StartDate.Value).TotalSeconds 
                    : null,
                ThreadId = Environment.CurrentManagedThreadId.ToString()
            };

            // Sign the entry
            entry.Signature = await SignAuditEntryAsync(entry);

            await _auditRepository.AddAsync(entry);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry");
        }
    }

    public async Task<PagedResult<AuditEntry>> GetAuditLogAsync(AuditSearchFilter filter, int page = 1, int pageSize = 15)
    {
        return await _auditRepository.SearchAsync(filter, page, pageSize);
    }

    public async Task<AuditStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // This would need to be implemented in the repository
        var stats = new AuditStatistics();
        return await Task.FromResult(stats);
    }

    public async Task<AuditEntry?> GetLastAuthAsync(string user, string? realm = null)
    {
        return await _auditRepository.GetLastAuthAsync(user, realm);
    }

    public async Task<int> CountAuthenticationsAsync(string user, string? realm = null, bool? success = null, DateTime? since = null)
    {
        return await _auditRepository.CountAuthenticationsAsync(user, realm, success, since);
    }

    public Task<string> SignAuditEntryAsync(AuditEntry entry)
    {
        // Create a simple signature from entry data
        var data = $"{entry.Date:O}|{entry.Action}|{entry.Success}|{entry.User}|{entry.Realm}|{entry.Serial}";
        var hash = _cryptoService.Sha256(System.Text.Encoding.UTF8.GetBytes(data));
        return Task.FromResult(Convert.ToBase64String(hash));
    }

    public Task<bool> VerifySignatureAsync(AuditEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Signature))
            return Task.FromResult(false);

        var data = $"{entry.Date:O}|{entry.Action}|{entry.Success}|{entry.User}|{entry.Realm}|{entry.Serial}";
        var hash = _cryptoService.Sha256(System.Text.Encoding.UTF8.GetBytes(data));
        var expectedSignature = Convert.ToBase64String(hash);

        return Task.FromResult(_cryptoService.SecureCompare(entry.Signature, expectedSignature));
    }

    public async Task<int> CleanupAsync(DateTime olderThan)
    {
        var count = await _auditRepository.CleanupAsync(olderThan);
        _logger.LogInformation("Cleaned up {Count} audit entries older than {Date}", count, olderThan);
        return count;
    }
}
