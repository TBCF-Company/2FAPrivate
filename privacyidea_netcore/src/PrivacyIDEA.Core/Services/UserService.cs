using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Core.Resolvers;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// User Service implementation
/// Maps to Python: privacyidea/lib/user.py
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly Dictionary<string, IUserResolver> _resolvers;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _resolvers = new Dictionary<string, IUserResolver>();
    }

    public async Task<UserInfo?> GetUserAsync(string username, string? realm = null)
    {
        var resolverNames = await GetResolversForRealmAsync(realm ?? await GetDefaultRealmAsync() ?? "");

        foreach (var resolverName in resolverNames)
        {
            var user = await GetUserFromResolverAsync(username, resolverName);
            if (user != null)
            {
                user.Realm = realm;
                return user;
            }
        }

        return null;
    }

    public async Task<PagedResult<UserInfo>> SearchUsersAsync(UserSearchFilter filter, int page = 1, int pageSize = 15)
    {
        var results = new List<UserInfo>();
        var resolvers = filter.Resolver != null 
            ? new[] { filter.Resolver }
            : (filter.Realm != null 
                ? await GetResolversForRealmAsync(filter.Realm) 
                : await GetAllResolverNamesAsync());

        foreach (var resolverName in resolvers)
        {
            var resolver = await GetResolverAsync(resolverName);
            if (resolver == null) continue;

            var searchParams = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(filter.Username))
                searchParams["username"] = filter.Username;
            if (!string.IsNullOrEmpty(filter.Email))
                searchParams["email"] = filter.Email;
            if (!string.IsNullOrEmpty(filter.GivenName))
                searchParams["givenname"] = filter.GivenName;
            if (!string.IsNullOrEmpty(filter.Surname))
                searchParams["surname"] = filter.Surname;

            var users = await resolver.GetUsersAsync(searchParams);
            foreach (var user in users)
            {
                user.Resolver = resolverName;
                user.Realm = filter.Realm;
                results.Add(user);
            }
        }

        // Apply pagination
        var total = results.Count;
        var items = results
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<UserInfo>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserInfo?> GetUserFromResolverAsync(string username, string resolverName)
    {
        var resolver = await GetResolverAsync(resolverName);
        if (resolver == null) return null;

        var userId = await resolver.GetUserIdAsync(username);
        if (string.IsNullOrEmpty(userId)) return null;

        var attributes = await resolver.GetUserAttributesAsync(userId);
        
        return new UserInfo
        {
            Username = username,
            UserId = userId,
            Resolver = resolverName,
            Email = attributes.GetValueOrDefault("email"),
            GivenName = attributes.GetValueOrDefault("givenname"),
            Surname = attributes.GetValueOrDefault("surname"),
            Phone = attributes.GetValueOrDefault("phone"),
            Mobile = attributes.GetValueOrDefault("mobile"),
            Description = attributes.GetValueOrDefault("description"),
            Editable = resolver.IsEditable,
            Attributes = attributes
        };
    }

    public async Task<IEnumerable<UserInfo>> GetUsersInRealmAsync(string realm)
    {
        var result = await SearchUsersAsync(new UserSearchFilter { Realm = realm }, 1, 1000);
        return result.Items;
    }

    public async Task<UserInfo> CreateUserAsync(UserCreateRequest request)
    {
        var resolverName = request.Resolver;
        if (string.IsNullOrEmpty(resolverName))
        {
            var resolvers = await GetResolversForRealmAsync(request.Realm ?? await GetDefaultRealmAsync() ?? "");
            resolverName = resolvers.FirstOrDefault();
        }

        if (string.IsNullOrEmpty(resolverName))
        {
            throw new InvalidOperationException("No resolver found for user creation");
        }

        var resolver = await GetResolverAsync(resolverName);
        if (resolver == null)
        {
            throw new InvalidOperationException($"Resolver '{resolverName}' not found");
        }

        if (!resolver.IsEditable)
        {
            throw new InvalidOperationException($"Resolver '{resolverName}' is not editable");
        }

        var attributes = new Dictionary<string, string>
        {
            ["username"] = request.Username
        };

        if (!string.IsNullOrEmpty(request.Email))
            attributes["email"] = request.Email;
        if (!string.IsNullOrEmpty(request.GivenName))
            attributes["givenname"] = request.GivenName;
        if (!string.IsNullOrEmpty(request.Surname))
            attributes["surname"] = request.Surname;
        if (!string.IsNullOrEmpty(request.Phone))
            attributes["phone"] = request.Phone;
        if (!string.IsNullOrEmpty(request.Mobile))
            attributes["mobile"] = request.Mobile;
        if (!string.IsNullOrEmpty(request.Description))
            attributes["description"] = request.Description;
        if (!string.IsNullOrEmpty(request.Password))
            attributes["password"] = request.Password;

        if (request.Attributes != null)
        {
            foreach (var attr in request.Attributes)
            {
                attributes[attr.Key] = attr.Value;
            }
        }

        var userId = await resolver.AddUserAsync(attributes);

        return new UserInfo
        {
            Username = request.Username,
            UserId = userId,
            Resolver = resolverName,
            Realm = request.Realm,
            Email = request.Email,
            GivenName = request.GivenName,
            Surname = request.Surname,
            Phone = request.Phone,
            Mobile = request.Mobile,
            Description = request.Description,
            Editable = true,
            Attributes = attributes
        };
    }

    public async Task<bool> UpdateUserAsync(string username, string? realm, UserUpdateRequest request)
    {
        var user = await GetUserAsync(username, realm);
        if (user == null) return false;

        var resolver = await GetResolverAsync(user.Resolver ?? "");
        if (resolver == null || !resolver.IsEditable) return false;

        var attributes = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(request.Email))
            attributes["email"] = request.Email;
        if (!string.IsNullOrEmpty(request.GivenName))
            attributes["givenname"] = request.GivenName;
        if (!string.IsNullOrEmpty(request.Surname))
            attributes["surname"] = request.Surname;
        if (!string.IsNullOrEmpty(request.Phone))
            attributes["phone"] = request.Phone;
        if (!string.IsNullOrEmpty(request.Mobile))
            attributes["mobile"] = request.Mobile;
        if (!string.IsNullOrEmpty(request.Description))
            attributes["description"] = request.Description;
        if (!string.IsNullOrEmpty(request.Password))
            attributes["password"] = request.Password;

        if (request.Attributes != null)
        {
            foreach (var attr in request.Attributes)
            {
                attributes[attr.Key] = attr.Value;
            }
        }

        return await resolver.UpdateUserAsync(user.UserId ?? username, attributes);
    }

    public async Task<bool> DeleteUserAsync(string username, string? realm)
    {
        var user = await GetUserAsync(username, realm);
        if (user == null) return false;

        var resolver = await GetResolverAsync(user.Resolver ?? "");
        if (resolver == null || !resolver.IsEditable) return false;

        return await resolver.DeleteUserAsync(user.UserId ?? username);
    }

    public async Task<Dictionary<string, string>> GetUserAttributesAsync(string username, string? realm = null)
    {
        var user = await GetUserAsync(username, realm);
        if (user == null) return new Dictionary<string, string>();

        var resolver = await GetResolverAsync(user.Resolver ?? "");
        if (resolver == null) return user.Attributes;

        return await resolver.GetUserAttributesAsync(user.UserId ?? username);
    }

    public async Task<bool> SetUserAttributeAsync(string username, string? realm, string key, string value)
    {
        var user = await GetUserAsync(username, realm);
        if (user == null) return false;

        // Store custom attributes in database
        var customAttr = new CustomUserAttribute
        {
            Username = username,
            Realm = realm ?? "",
            AttributeKey = key,
            AttributeValue = value
        };

        // Check if exists
        var existing = await GetCustomUserAttributeAsync(username, realm, key);
        if (existing != null)
        {
            existing.AttributeValue = value;
            _unitOfWork.Repository<CustomUserAttribute>().Update(existing);
        }
        else
        {
            await _unitOfWork.Repository<CustomUserAttribute>().AddAsync(customAttr);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAttributeAsync(string username, string? realm, string key)
    {
        var existing = await GetCustomUserAttributeAsync(username, realm, key);
        if (existing == null) return false;

        _unitOfWork.Repository<CustomUserAttribute>().Remove(existing);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UserExistsAsync(string username, string? realm = null)
    {
        var user = await GetUserAsync(username, realm);
        return user != null;
    }

    public async Task<IEnumerable<string>> GetResolversForRealmAsync(string realm)
    {
        if (string.IsNullOrEmpty(realm))
        {
            return await GetAllResolverNamesAsync();
        }

        var realmEntity = await GetRealmAsync(realm);
        if (realmEntity == null) return Enumerable.Empty<string>();

        var resolverRealms = await _unitOfWork.Repository<ResolverRealm>()
            .FindAsync(rr => rr.RealmId == realmEntity.Id);

        var resolverIds = resolverRealms.Select(rr => rr.ResolverId).ToList();
        var resolvers = await _unitOfWork.Repository<Resolver>().GetAllAsync();

        return resolvers
            .Where(r => resolverIds.Contains(r.Id))
            .Select(r => r.Name);
    }

    public async Task<IEnumerable<string>> GetRealmsAsync()
    {
        var realms = await _unitOfWork.Repository<Realm>().GetAllAsync();
        return realms.Select(r => r.Name);
    }

    public async Task<string?> GetDefaultRealmAsync()
    {
        var realms = await _unitOfWork.Repository<Realm>().GetAllAsync();
        var defaultRealm = realms.FirstOrDefault(r => r.IsDefault);
        return defaultRealm?.Name;
    }

    private async Task<Realm?> GetRealmAsync(string name)
    {
        var realms = await _unitOfWork.Repository<Realm>().GetAllAsync();
        return realms.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IEnumerable<string>> GetAllResolverNamesAsync()
    {
        var resolvers = await _unitOfWork.Repository<Resolver>().GetAllAsync();
        return resolvers.Select(r => r.Name);
    }

    private async Task<IUserResolver?> GetResolverAsync(string name)
    {
        if (_resolvers.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var resolvers = await _unitOfWork.Repository<Resolver>().GetAllAsync();
        var resolverEntity = resolvers.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (resolverEntity == null) return null;

        // Get resolver config
        var configs = await _unitOfWork.Repository<ResolverConfig>()
            .FindAsync(c => c.ResolverId == resolverEntity.Id);

        var configDict = configs.ToDictionary(c => c.Key, c => c.Value);

        // Create resolver instance based on type (using null logger for simplicity)
        // In production, resolvers should be injected via DI
        IUserResolver? resolver = null;
        
        // Return null for now - resolvers should be properly instantiated via DI
        // This is a temporary workaround until full resolver DI is implemented
        
        if (resolver != null)
        {
            _resolvers[name] = resolver;
        }

        return resolver;
    }

    private async Task<CustomUserAttribute?> GetCustomUserAttributeAsync(string username, string? realm, string key)
    {
        var attrs = await _unitOfWork.Repository<CustomUserAttribute>()
            .FindAsync(a => a.Username == username && 
                           a.Realm == (realm ?? "") && 
                           a.AttributeKey == key);
        return attrs.FirstOrDefault();
    }
}

/// <summary>
/// Custom user attribute entity for storing additional user data
/// </summary>
public class CustomUserAttribute
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string AttributeKey { get; set; } = string.Empty;
    public string AttributeValue { get; set; } = string.Empty;
}
