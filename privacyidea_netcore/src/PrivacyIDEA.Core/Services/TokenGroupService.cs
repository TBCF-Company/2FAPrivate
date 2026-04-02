using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// Service for managing token groups
/// </summary>
public class TokenGroupService : ITokenGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TokenGroupService> _logger;

    public TokenGroupService(IUnitOfWork unitOfWork, ILogger<TokenGroupService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<TokenGroupInfo>> GetGroupsAsync()
    {
        var groups = await _unitOfWork.Query<TokenGroup>()
            .Include(g => g.TokenAssociations)
            .ThenInclude(ta => ta.Token)
            .ToListAsync();

        return groups.Select(MapToInfo);
    }

    public async Task<TokenGroupInfo?> GetGroupAsync(string name)
    {
        var group = await _unitOfWork.Query<TokenGroup>()
            .Include(g => g.TokenAssociations)
            .ThenInclude(ta => ta.Token)
            .FirstOrDefaultAsync(g => g.Name == name);

        return group != null ? MapToInfo(group) : null;
    }

    public async Task<TokenGroupInfo> CreateGroupAsync(string name, string? description = null)
    {
        var existing = await _unitOfWork.Query<TokenGroup>()
            .FirstOrDefaultAsync(g => g.Name == name);

        if (existing != null)
            throw new InvalidOperationException($"Token group '{name}' already exists");

        var group = new TokenGroup
        {
            Name = name,
            Description = description
        };

        _unitOfWork.Add(group);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Created token group: {Name}", name);
        return MapToInfo(group);
    }

    public async Task<TokenGroupInfo?> UpdateGroupAsync(string name, string? description)
    {
        var group = await _unitOfWork.Query<TokenGroup>()
            .FirstOrDefaultAsync(g => g.Name == name);

        if (group == null)
            return null;

        group.Description = description;
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Updated token group: {Name}", name);
        return MapToInfo(group);
    }

    public async Task<bool> DeleteGroupAsync(string name)
    {
        var group = await _unitOfWork.Query<TokenGroup>()
            .Include(g => g.TokenAssociations)
            .FirstOrDefaultAsync(g => g.Name == name);

        if (group == null)
            return false;

        _unitOfWork.Delete(group);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Deleted token group: {Name}", name);
        return true;
    }

    public async Task<bool> AddTokenToGroupAsync(string groupName, string tokenSerial)
    {
        var group = await _unitOfWork.Query<TokenGroup>()
            .FirstOrDefaultAsync(g => g.Name == groupName);

        if (group == null)
            return false;

        var token = await _unitOfWork.Query<Token>()
            .FirstOrDefaultAsync(t => t.Serial == tokenSerial);

        if (token == null)
            return false;

        var existing = await _unitOfWork.Query<TokenTokenGroup>()
            .FirstOrDefaultAsync(ttg => ttg.TokenGroupId == group.Id && ttg.TokenId == token.Id);

        if (existing != null)
            return true; // Already in group

        var association = new TokenTokenGroup
        {
            TokenId = token.Id,
            TokenGroupId = group.Id
        };

        _unitOfWork.Add(association);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Added token {Serial} to group {Group}", tokenSerial, groupName);
        return true;
    }

    public async Task<bool> RemoveTokenFromGroupAsync(string groupName, string tokenSerial)
    {
        var group = await _unitOfWork.Query<TokenGroup>()
            .FirstOrDefaultAsync(g => g.Name == groupName);

        if (group == null)
            return false;

        var token = await _unitOfWork.Query<Token>()
            .FirstOrDefaultAsync(t => t.Serial == tokenSerial);

        if (token == null)
            return false;

        var association = await _unitOfWork.Query<TokenTokenGroup>()
            .FirstOrDefaultAsync(ttg => ttg.TokenGroupId == group.Id && ttg.TokenId == token.Id);

        if (association == null)
            return false;

        _unitOfWork.Delete(association);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Removed token {Serial} from group {Group}", tokenSerial, groupName);
        return true;
    }

    public async Task<IEnumerable<string>> GetTokensInGroupAsync(string groupName)
    {
        var group = await _unitOfWork.Query<TokenGroup>()
            .Include(g => g.TokenAssociations)
            .ThenInclude(ta => ta.Token)
            .FirstOrDefaultAsync(g => g.Name == groupName);

        if (group == null)
            return Array.Empty<string>();

        return group.TokenAssociations
            .Where(ta => ta.Token != null)
            .Select(ta => ta.Token!.Serial);
    }

    public async Task<IEnumerable<string>> GetGroupsForTokenAsync(string tokenSerial)
    {
        var token = await _unitOfWork.Query<Token>()
            .Include(t => t.TokenGroups)
            .ThenInclude(ttg => ttg.TokenGroup)
            .FirstOrDefaultAsync(t => t.Serial == tokenSerial);

        if (token == null)
            return Array.Empty<string>();

        return token.TokenGroups
            .Where(ttg => ttg.TokenGroup != null)
            .Select(ttg => ttg.TokenGroup!.Name);
    }

    private static TokenGroupInfo MapToInfo(TokenGroup group)
    {
        var tokenSerials = group.TokenAssociations
            .Where(ta => ta.Token != null)
            .Select(ta => ta.Token!.Serial)
            .ToList();

        return new TokenGroupInfo
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            TokenCount = tokenSerials.Count,
            TokenSerials = tokenSerials
        };
    }
}
