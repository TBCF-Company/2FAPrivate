using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Repository interface for Token entity
/// </summary>
public interface ITokenRepository
{
    Task<Token?> GetBySerialAsync(string serial);
    Task<Token?> GetByIdAsync(int id);
    Task<IEnumerable<Token>> GetForUserAsync(string userId, string? realm = null);
    Task<PagedResult<Token>> SearchAsync(TokenSearchFilter filter, int page, int pageSize);
    Task<Token> AddAsync(Token token);
    Task UpdateAsync(Token token);
    Task DeleteAsync(Token token);
    Task<int> CountAsync();
    Task<int> CountActiveAsync();
    Task<int> CountAssignedAsync();
    Task<Dictionary<string, int>> CountByTypeAsync();
}

/// <summary>
/// Repository interface for Realm entity
/// </summary>
public interface IRealmRepository
{
    Task<Realm?> GetByNameAsync(string name);
    Task<Realm?> GetByIdAsync(int id);
    Task<IEnumerable<Realm>> GetAllAsync();
    Task<Realm?> GetDefaultAsync();
    Task<Realm> AddAsync(Realm realm);
    Task UpdateAsync(Realm realm);
    Task DeleteAsync(Realm realm);
}

/// <summary>
/// Repository interface for Resolver entity
/// </summary>
public interface IResolverRepository
{
    Task<Resolver?> GetByNameAsync(string name);
    Task<Resolver?> GetByIdAsync(int id);
    Task<IEnumerable<Resolver>> GetAllAsync();
    Task<Resolver> AddAsync(Resolver resolver);
    Task UpdateAsync(Resolver resolver);
    Task DeleteAsync(Resolver resolver);
    Task<IEnumerable<Resolver>> GetByRealmAsync(string realmName);
}

/// <summary>
/// Repository interface for Policy entity
/// </summary>
public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(int id);
    Task<Policy?> GetByNameAsync(string name);
    Task<IEnumerable<Policy>> GetAllAsync(bool? active = null);
    Task<IEnumerable<Policy>> GetByScopeAsync(string scope, bool? active = null);
    Task<Policy> AddAsync(Policy policy);
    Task UpdateAsync(Policy policy);
    Task DeleteAsync(Policy policy);
}

/// <summary>
/// Repository interface for AuditEntry entity
/// </summary>
public interface IAuditRepository
{
    Task<AuditEntry> AddAsync(AuditEntry entry);
    Task<PagedResult<AuditEntry>> SearchAsync(AuditSearchFilter filter, int page, int pageSize);
    Task<AuditEntry?> GetLastAuthAsync(string user, string? realm);
    Task<int> CountAuthenticationsAsync(string user, string? realm, bool? success, DateTime? since);
    Task<int> CleanupAsync(DateTime olderThan);
}

/// <summary>
/// Repository interface for Challenge entity
/// </summary>
public interface IChallengeRepository
{
    Task<Challenge?> GetByTransactionIdAsync(string transactionId);
    Task<Challenge> AddAsync(Challenge challenge);
    Task UpdateAsync(Challenge challenge);
    Task DeleteAsync(Challenge challenge);
    Task CleanupExpiredAsync();
}

/// <summary>
/// Unit of Work interface
/// </summary>
public interface IUnitOfWork
{
    ITokenRepository Tokens { get; }
    IRealmRepository Realms { get; }
    IResolverRepository Resolvers { get; }
    IPolicyRepository Policies { get; }
    IAuditRepository Audit { get; }
    IChallengeRepository Challenges { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
