using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// Service for managing token containers
/// </summary>
public class ContainerService : IContainerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ContainerService> _logger;

    public ContainerService(IUnitOfWork unitOfWork, ILogger<ContainerService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<ContainerInfo>> GetContainersAsync(string? realm = null, string? user = null)
    {
        var query = _unitOfWork.Query<TokenContainer>()
            .Include(c => c.Owners)
            .Include(c => c.Tokens).ThenInclude(t => t.Token)
            .Include(c => c.Realms).ThenInclude(r => r.Realm)
            .Include(c => c.States)
            .Include(c => c.Infos)
            .AsQueryable();

        if (!string.IsNullOrEmpty(realm))
        {
            query = query.Where(c => c.Realms.Any(r => r.Realm != null && r.Realm.Name == realm));
        }

        if (!string.IsNullOrEmpty(user))
        {
            query = query.Where(c => c.Owners.Any(o => o.UserId == user));
        }

        var containers = await query.ToListAsync();
        return containers.Select(MapToInfo);
    }

    public async Task<ContainerInfo?> GetContainerAsync(string serial)
    {
        var container = await _unitOfWork.Query<TokenContainer>()
            .Include(c => c.Owners).ThenInclude(o => o.Realm)
            .Include(c => c.Owners).ThenInclude(o => o.Resolver)
            .Include(c => c.Tokens).ThenInclude(t => t.Token)
            .Include(c => c.Realms).ThenInclude(r => r.Realm)
            .Include(c => c.States)
            .Include(c => c.Infos)
            .FirstOrDefaultAsync(c => c.Serial == serial);

        return container != null ? MapToInfo(container) : null;
    }

    public async Task<ContainerInfo> CreateContainerAsync(CreateContainerRequest request)
    {
        var serial = request.Serial ?? GenerateContainerSerial();

        var existing = await _unitOfWork.Query<TokenContainer>()
            .FirstOrDefaultAsync(c => c.Serial == serial);

        if (existing != null)
            throw new InvalidOperationException($"Container with serial '{serial}' already exists");

        var container = new TokenContainer
        {
            Serial = serial,
            Type = request.Type,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        // Add info
        if (request.Info != null)
        {
            foreach (var kvp in request.Info)
            {
                container.Infos.Add(new TokenContainerInfo
                {
                    Key = kvp.Key,
                    Value = kvp.Value
                });
            }
        }

        // Set initial state
        container.States.Add(new TokenContainerState
        {
            State = "active",
            Timestamp = DateTime.UtcNow
        });

        _unitOfWork.Add(container);
        await _unitOfWork.SaveChangesAsync();

        // Assign to user if specified
        if (!string.IsNullOrEmpty(request.UserId))
        {
            await AssignContainerAsync(serial, request.UserId, request.Realm);
        }

        _logger.LogInformation("Created container: {Serial}", serial);
        return (await GetContainerAsync(serial))!;
    }

    public async Task<ContainerInfo?> UpdateContainerAsync(string serial, UpdateContainerRequest request)
    {
        var container = await _unitOfWork.Query<TokenContainer>()
            .Include(c => c.Infos)
            .FirstOrDefaultAsync(c => c.Serial == serial);

        if (container == null)
            return null;

        if (request.Description != null)
        {
            container.Description = request.Description;
        }

        if (request.Info != null)
        {
            // Update or add info
            foreach (var kvp in request.Info)
            {
                var existing = container.Infos.FirstOrDefault(i => i.Key == kvp.Key);
                if (existing != null)
                {
                    existing.Value = kvp.Value;
                }
                else
                {
                    container.Infos.Add(new TokenContainerInfo
                    {
                        ContainerId = container.Id,
                        Key = kvp.Key,
                        Value = kvp.Value
                    });
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Updated container: {Serial}", serial);
        
        return await GetContainerAsync(serial);
    }

    public async Task<bool> DeleteContainerAsync(string serial)
    {
        var container = await _unitOfWork.Query<TokenContainer>()
            .FirstOrDefaultAsync(c => c.Serial == serial);

        if (container == null)
            return false;

        _unitOfWork.Delete(container);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Deleted container: {Serial}", serial);
        return true;
    }

    public async Task<bool> AddTokenToContainerAsync(string containerSerial, string tokenSerial)
    {
        var container = await _unitOfWork.Query<TokenContainer>()
            .FirstOrDefaultAsync(c => c.Serial == containerSerial);

        if (container == null)
            return false;

        var token = await _unitOfWork.Query<Token>()
            .FirstOrDefaultAsync(t => t.Serial == tokenSerial);

        if (token == null)
            return false;

        var existing = await _unitOfWork.Query<TokenContainerToken>()
            .FirstOrDefaultAsync(ct => ct.ContainerId == container.Id && ct.TokenId == token.Id);

        if (existing != null)
            return true;

        var association = new TokenContainerToken
        {
            ContainerId = container.Id,
            TokenId = token.Id
        };

        _unitOfWork.Add(association);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Added token {Token} to container {Container}", tokenSerial, containerSerial);
        return true;
    }

    public async Task<bool> RemoveTokenFromContainerAsync(string containerSerial, string tokenSerial)
    {
        var container = await _unitOfWork.Query<TokenContainer>()
            .FirstOrDefaultAsync(c => c.Serial == containerSerial);

        if (container == null)
            return false;

        var token = await _unitOfWork.Query<Token>()
            .FirstOrDefaultAsync(t => t.Serial == tokenSerial);

        if (token == null)
            return false;

        var association = await _unitOfWork.Query<TokenContainerToken>()
            .FirstOrDefaultAsync(ct => ct.ContainerId == container.Id && ct.TokenId == token.Id);

        if (association == null)
            return false;

        _unitOfWork.Delete(association);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Removed token {Token} from container {Container}", tokenSerial, containerSerial);
        return true;
    }

    public async Task<bool> SetContainerStateAsync(string serial, string state)
    {
        var container = await _unitOfWork.Query<TokenContainer>()
            .FirstOrDefaultAsync(c => c.Serial == serial);

        if (container == null)
            return false;

        var stateEntry = new TokenContainerState
        {
            ContainerId = container.Id,
            State = state,
            Timestamp = DateTime.UtcNow
        };

        _unitOfWork.Add(stateEntry);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Set container {Serial} state to {State}", serial, state);
        return true;
    }

    public async Task<bool> AssignContainerAsync(string serial, string userId, string? realm = null)
    {
        var container = await _unitOfWork.Query<TokenContainer>()
            .Include(c => c.Owners)
            .FirstOrDefaultAsync(c => c.Serial == serial);

        if (container == null)
            return false;

        // Remove existing owners
        foreach (var owner in container.Owners.ToList())
        {
            _unitOfWork.Delete(owner);
        }

        int? realmId = null;
        if (!string.IsNullOrEmpty(realm))
        {
            var realmEntity = await _unitOfWork.Query<Realm>()
                .FirstOrDefaultAsync(r => r.Name == realm);
            realmId = realmEntity?.Id;
        }

        var newOwner = new TokenContainerOwner
        {
            ContainerId = container.Id,
            UserId = userId,
            RealmId = realmId
        };

        _unitOfWork.Add(newOwner);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Assigned container {Serial} to user {User}", serial, userId);
        return true;
    }

    public async Task<bool> UnassignContainerAsync(string serial)
    {
        var container = await _unitOfWork.Query<TokenContainer>()
            .Include(c => c.Owners)
            .FirstOrDefaultAsync(c => c.Serial == serial);

        if (container == null)
            return false;

        foreach (var owner in container.Owners.ToList())
        {
            _unitOfWork.Delete(owner);
        }

        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Unassigned container {Serial}", serial);
        return true;
    }

    public async Task<IEnumerable<ContainerStateInfo>> GetContainerStatesAsync(string serial)
    {
        var container = await _unitOfWork.Query<TokenContainer>()
            .Include(c => c.States)
            .FirstOrDefaultAsync(c => c.Serial == serial);

        if (container == null)
            return Array.Empty<ContainerStateInfo>();

        return container.States
            .OrderByDescending(s => s.Timestamp)
            .Select(s => new ContainerStateInfo
            {
                State = s.State,
                Timestamp = s.Timestamp
            });
    }

    private static string GenerateContainerSerial()
    {
        return $"CONT{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    private static ContainerInfo MapToInfo(TokenContainer container)
    {
        var owner = container.Owners.FirstOrDefault();
        var currentState = container.States
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefault();

        return new ContainerInfo
        {
            Id = container.Id,
            Serial = container.Serial,
            Type = container.Type,
            Description = container.Description,
            CreatedAt = container.CreatedAt,
            CurrentState = currentState?.State,
            Owner = owner != null ? new ContainerOwnerInfo
            {
                UserId = owner.UserId,
                Realm = owner.Realm?.Name,
                Resolver = owner.Resolver?.Name
            } : null,
            TokenSerials = container.Tokens
                .Where(t => t.Token != null)
                .Select(t => t.Token!.Serial)
                .ToList(),
            Realms = container.Realms
                .Where(r => r.Realm != null)
                .Select(r => r.Realm!.Name)
                .ToList(),
            Info = container.Infos.ToDictionary(i => i.Key, i => i.Value ?? string.Empty)
        };
    }
}
