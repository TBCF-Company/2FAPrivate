namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Service for managing token containers
/// </summary>
public interface IContainerService
{
    /// <summary>
    /// Get all token containers
    /// </summary>
    Task<IEnumerable<ContainerInfo>> GetContainersAsync(string? realm = null, string? user = null);

    /// <summary>
    /// Get a specific container by serial
    /// </summary>
    Task<ContainerInfo?> GetContainerAsync(string serial);

    /// <summary>
    /// Create a token container
    /// </summary>
    Task<ContainerInfo> CreateContainerAsync(CreateContainerRequest request);

    /// <summary>
    /// Update a container
    /// </summary>
    Task<ContainerInfo?> UpdateContainerAsync(string serial, UpdateContainerRequest request);

    /// <summary>
    /// Delete a container
    /// </summary>
    Task<bool> DeleteContainerAsync(string serial);

    /// <summary>
    /// Add a token to a container
    /// </summary>
    Task<bool> AddTokenToContainerAsync(string containerSerial, string tokenSerial);

    /// <summary>
    /// Remove a token from a container
    /// </summary>
    Task<bool> RemoveTokenFromContainerAsync(string containerSerial, string tokenSerial);

    /// <summary>
    /// Set container state
    /// </summary>
    Task<bool> SetContainerStateAsync(string serial, string state);

    /// <summary>
    /// Assign container to user
    /// </summary>
    Task<bool> AssignContainerAsync(string serial, string userId, string? realm = null);

    /// <summary>
    /// Unassign container from user
    /// </summary>
    Task<bool> UnassignContainerAsync(string serial);

    /// <summary>
    /// Get container states history
    /// </summary>
    Task<IEnumerable<ContainerStateInfo>> GetContainerStatesAsync(string serial);
}

public class ContainerInfo
{
    public int Id { get; set; }
    public string Serial { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CurrentState { get; set; }
    public ContainerOwnerInfo? Owner { get; set; }
    public IEnumerable<string> TokenSerials { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Realms { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> Info { get; set; } = new();
}

public class ContainerOwnerInfo
{
    public string UserId { get; set; } = string.Empty;
    public string? Realm { get; set; }
    public string? Resolver { get; set; }
}

public class CreateContainerRequest
{
    public string? Serial { get; set; }
    public string Type { get; set; } = "generic";
    public string? Description { get; set; }
    public string? UserId { get; set; }
    public string? Realm { get; set; }
    public Dictionary<string, string>? Info { get; set; }
}

public class UpdateContainerRequest
{
    public string? Description { get; set; }
    public Dictionary<string, string>? Info { get; set; }
}

public class ContainerStateInfo
{
    public string State { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
