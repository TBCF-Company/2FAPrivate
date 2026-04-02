namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Machine service interface for managing machine-token associations
/// Maps to Python: privacyidea/lib/machine.py
/// </summary>
public interface IMachineService
{
    Task<IEnumerable<MachineInfo>> GetMachinesAsync(string? hostname = null);
    Task<int> CreateMachineAsync(MachineInfo machine);
    Task<bool> DeleteMachineAsync(string hostname);
    Task<bool> AttachTokenAsync(string hostname, string serial, string application, Dictionary<string, string>? options = null);
    Task<bool> DetachTokenAsync(string hostname, string serial, string application);
    Task<IEnumerable<MachineTokenInfo>> GetMachineTokensAsync(string? hostname = null, string? serial = null);
    Task<IEnumerable<AuthItemInfo>> GetAuthItemsAsync(string application, string? hostname = null);
}

/// <summary>
/// Machine information
/// </summary>
public class MachineInfo
{
    public int Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string? Ip { get; set; }
    public string? Resolver { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Machine-token association
/// </summary>
public class MachineTokenInfo
{
    public string Hostname { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public Dictionary<string, string> Options { get; set; } = new();
}

/// <summary>
/// Authentication item (SSH key, LUKS key, etc.)
/// </summary>
public class AuthItemInfo
{
    public string Hostname { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Value { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}
