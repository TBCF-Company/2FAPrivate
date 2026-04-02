using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Infrastructure.Data;

/// <summary>
/// Token Repository implementation
/// </summary>
public class TokenRepository : ITokenRepository
{
    private readonly PrivacyIdeaDbContext _context;

    public TokenRepository(PrivacyIdeaDbContext context)
    {
        _context = context;
    }

    public async Task<Token?> GetBySerialAsync(string serial)
    {
        return await _context.Tokens
            .Include(t => t.TokenInfos)
            .Include(t => t.TokenOwners).ThenInclude(o => o.Realm)
            .Include(t => t.TokenRealms).ThenInclude(tr => tr.Realm)
            .FirstOrDefaultAsync(t => t.Serial == serial);
    }

    public async Task<Token?> GetByIdAsync(int id)
    {
        return await _context.Tokens
            .Include(t => t.TokenInfos)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Token>> GetForUserAsync(string userId, string? realm = null)
    {
        var query = _context.Tokens
            .Include(t => t.TokenInfos)
            .Include(t => t.TokenOwners).ThenInclude(o => o.Realm)
            .Where(t => t.TokenOwners.Any(o => o.UserId == userId));

        if (!string.IsNullOrEmpty(realm))
        {
            query = query.Where(t => t.TokenOwners.Any(o => o.Realm != null && o.Realm.Name == realm));
        }

        return await query.ToListAsync();
    }

    public async Task<PagedResult<Token>> SearchAsync(TokenSearchFilter filter, int page, int pageSize)
    {
        var query = _context.Tokens
            .Include(t => t.TokenInfos)
            .Include(t => t.TokenOwners)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.Serial))
            query = query.Where(t => t.Serial.Contains(filter.Serial));

        if (!string.IsNullOrEmpty(filter.TokenType))
            query = query.Where(t => t.TokenType == filter.TokenType);

        if (filter.Active.HasValue)
            query = query.Where(t => t.Active == filter.Active.Value);

        if (!string.IsNullOrEmpty(filter.UserId))
            query = query.Where(t => t.TokenOwners.Any(o => o.UserId == filter.UserId));

        if (filter.Assigned.HasValue)
        {
            if (filter.Assigned.Value)
                query = query.Where(t => t.TokenOwners.Any());
            else
                query = query.Where(t => !t.TokenOwners.Any());
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Token>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Token> AddAsync(Token token)
    {
        _context.Tokens.Add(token);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task UpdateAsync(Token token)
    {
        _context.Tokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Token token)
    {
        _context.Tokens.Remove(token);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.Tokens.CountAsync();
    }

    public async Task<int> CountActiveAsync()
    {
        return await _context.Tokens.CountAsync(t => t.Active);
    }

    public async Task<int> CountAssignedAsync()
    {
        return await _context.Tokens.CountAsync(t => t.TokenOwners.Any());
    }

    public async Task<Dictionary<string, int>> CountByTypeAsync()
    {
        return await _context.Tokens
            .GroupBy(t => t.TokenType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);
    }
}

/// <summary>
/// Realm Repository implementation
/// </summary>
public class RealmRepository : IRealmRepository
{
    private readonly PrivacyIdeaDbContext _context;

    public RealmRepository(PrivacyIdeaDbContext context)
    {
        _context = context;
    }

    public async Task<Realm?> GetByNameAsync(string name)
    {
        return await _context.Realms
            .Include(r => r.ResolverRealms)
            .ThenInclude(rr => rr.Resolver)
            .FirstOrDefaultAsync(r => r.Name == name.ToLower());
    }

    public async Task<Realm?> GetByIdAsync(int id)
    {
        return await _context.Realms
            .Include(r => r.ResolverRealms)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Realm>> GetAllAsync()
    {
        return await _context.Realms
            .Include(r => r.ResolverRealms)
            .ThenInclude(rr => rr.Resolver)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Realm?> GetDefaultAsync()
    {
        return await _context.Realms
            .Include(r => r.ResolverRealms)
            .FirstOrDefaultAsync(r => r.IsDefault);
    }

    public async Task<Realm> AddAsync(Realm realm)
    {
        _context.Realms.Add(realm);
        await _context.SaveChangesAsync();
        return realm;
    }

    public async Task UpdateAsync(Realm realm)
    {
        _context.Realms.Update(realm);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Realm realm)
    {
        _context.Realms.Remove(realm);
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Resolver Repository implementation
/// </summary>
public class ResolverRepository : IResolverRepository
{
    private readonly PrivacyIdeaDbContext _context;

    public ResolverRepository(PrivacyIdeaDbContext context)
    {
        _context = context;
    }

    public async Task<Resolver?> GetByNameAsync(string name)
    {
        return await _context.Resolvers
            .Include(r => r.Configs)
            .FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<Resolver?> GetByIdAsync(int id)
    {
        return await _context.Resolvers
            .Include(r => r.Configs)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Resolver>> GetAllAsync()
    {
        return await _context.Resolvers
            .Include(r => r.Configs)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Resolver> AddAsync(Resolver resolver)
    {
        _context.Resolvers.Add(resolver);
        await _context.SaveChangesAsync();
        return resolver;
    }

    public async Task UpdateAsync(Resolver resolver)
    {
        _context.Resolvers.Update(resolver);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Resolver resolver)
    {
        _context.Resolvers.Remove(resolver);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Resolver>> GetByRealmAsync(string realmName)
    {
        return await _context.ResolverRealms
            .Include(rr => rr.Resolver)
            .ThenInclude(r => r!.Configs)
            .Where(rr => rr.Realm != null && rr.Realm.Name == realmName)
            .OrderBy(rr => rr.Priority)
            .Select(rr => rr.Resolver!)
            .ToListAsync();
    }
}

/// <summary>
/// Policy Repository implementation
/// </summary>
public class PolicyRepository : IPolicyRepository
{
    private readonly PrivacyIdeaDbContext _context;

    public PolicyRepository(PrivacyIdeaDbContext context)
    {
        _context = context;
    }

    public async Task<Policy?> GetByIdAsync(int id)
    {
        return await _context.Policies
            .Include(p => p.Conditions)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Policy?> GetByNameAsync(string name)
    {
        return await _context.Policies
            .Include(p => p.Conditions)
            .FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<IEnumerable<Policy>> GetAllAsync(bool? active = null)
    {
        var query = _context.Policies.Include(p => p.Conditions).AsQueryable();
        
        if (active.HasValue)
            query = query.Where(p => p.Active == active.Value);
        
        return await query.OrderBy(p => p.Priority).ThenBy(p => p.Name).ToListAsync();
    }

    public async Task<IEnumerable<Policy>> GetByScopeAsync(string scope, bool? active = null)
    {
        var query = _context.Policies
            .Include(p => p.Conditions)
            .Where(p => p.Scope == scope);

        if (active.HasValue)
            query = query.Where(p => p.Active == active.Value);

        return await query.OrderBy(p => p.Priority).ToListAsync();
    }

    public async Task<Policy> AddAsync(Policy policy)
    {
        _context.Policies.Add(policy);
        await _context.SaveChangesAsync();
        return policy;
    }

    public async Task UpdateAsync(Policy policy)
    {
        _context.Policies.Update(policy);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Policy policy)
    {
        if (policy.Conditions.Any())
        {
            _context.PolicyConditions.RemoveRange(policy.Conditions);
        }
        _context.Policies.Remove(policy);
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Audit Repository implementation
/// </summary>
public class AuditRepository : IAuditRepository
{
    private readonly PrivacyIdeaDbContext _context;

    public AuditRepository(PrivacyIdeaDbContext context)
    {
        _context = context;
    }

    public async Task<AuditEntry> AddAsync(AuditEntry entry)
    {
        _context.AuditEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<PagedResult<AuditEntry>> SearchAsync(AuditSearchFilter filter, int page, int pageSize)
    {
        var query = _context.AuditEntries.AsQueryable();

        if (!string.IsNullOrEmpty(filter.Serial))
            query = query.Where(a => a.Serial != null && a.Serial.Contains(filter.Serial));

        if (!string.IsNullOrEmpty(filter.User))
            query = query.Where(a => a.User != null && a.User.Contains(filter.User));

        if (!string.IsNullOrEmpty(filter.Realm))
            query = query.Where(a => a.Realm == filter.Realm);

        if (!string.IsNullOrEmpty(filter.Action))
            query = query.Where(a => a.Action != null && a.Action.Contains(filter.Action));

        if (filter.Success.HasValue)
            query = query.Where(a => a.Success == filter.Success.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(a => a.Date >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(a => a.Date <= filter.EndDate.Value);

        query = filter.SortOrder?.ToLower() == "asc"
            ? query.OrderBy(a => a.Date)
            : query.OrderByDescending(a => a.Date);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AuditEntry>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AuditEntry?> GetLastAuthAsync(string user, string? realm)
    {
        var query = _context.AuditEntries
            .Where(a => a.User == user && a.Action == "validate/check" && a.Success);

        if (!string.IsNullOrEmpty(realm))
            query = query.Where(a => a.Realm == realm);

        return await query.OrderByDescending(a => a.Date).FirstOrDefaultAsync();
    }

    public async Task<int> CountAuthenticationsAsync(string user, string? realm, bool? success, DateTime? since)
    {
        var query = _context.AuditEntries
            .Where(a => a.User == user && a.Action == "validate/check");

        if (!string.IsNullOrEmpty(realm))
            query = query.Where(a => a.Realm == realm);

        if (success.HasValue)
            query = query.Where(a => a.Success == success.Value);

        if (since.HasValue)
            query = query.Where(a => a.Date >= since.Value);

        return await query.CountAsync();
    }

    public async Task<int> CleanupAsync(DateTime olderThan)
    {
        var oldEntries = await _context.AuditEntries
            .Where(a => a.Date < olderThan)
            .ToListAsync();

        _context.AuditEntries.RemoveRange(oldEntries);
        await _context.SaveChangesAsync();

        return oldEntries.Count;
    }
}

/// <summary>
/// Challenge Repository implementation
/// </summary>
public class ChallengeRepository : IChallengeRepository
{
    private readonly PrivacyIdeaDbContext _context;

    public ChallengeRepository(PrivacyIdeaDbContext context)
    {
        _context = context;
    }

    public async Task<Challenge?> GetByTransactionIdAsync(string transactionId)
    {
        return await _context.Challenges.FirstOrDefaultAsync(c => c.TransactionId == transactionId);
    }

    public async Task<Challenge> AddAsync(Challenge challenge)
    {
        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();
        return challenge;
    }

    public async Task UpdateAsync(Challenge challenge)
    {
        _context.Challenges.Update(challenge);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Challenge challenge)
    {
        _context.Challenges.Remove(challenge);
        await _context.SaveChangesAsync();
    }

    public async Task CleanupExpiredAsync()
    {
        var expired = await _context.Challenges
            .Where(c => c.Expiration < DateTime.UtcNow)
            .ToListAsync();

        _context.Challenges.RemoveRange(expired);
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Unit of Work implementation
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly PrivacyIdeaDbContext _context;

    public ITokenRepository Tokens { get; }
    public IRealmRepository Realms { get; }
    public IResolverRepository Resolvers { get; }
    public IPolicyRepository Policies { get; }
    public IAuditRepository Audit { get; }
    public IChallengeRepository Challenges { get; }

    public UnitOfWork(PrivacyIdeaDbContext context)
    {
        _context = context;
        Tokens = new TokenRepository(context);
        Realms = new RealmRepository(context);
        Resolvers = new ResolverRepository(context);
        Policies = new PolicyRepository(context);
        Audit = new AuditRepository(context);
        Challenges = new ChallengeRepository(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
