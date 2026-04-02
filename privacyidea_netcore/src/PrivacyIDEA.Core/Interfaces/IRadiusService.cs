namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// RADIUS Server service interface
/// Maps to Python: privacyidea/lib/radiusserver.py
/// </summary>
public interface IRadiusService
{
    Task<IEnumerable<RadiusServerInfo>> GetAllServersAsync();
    Task<RadiusServerInfo?> GetServerAsync(string identifier);
    Task<int> CreateServerAsync(RadiusServerConfig config);
    Task<bool> DeleteServerAsync(string identifier);
    Task<RadiusTestResult> TestConnectionAsync(string identifier, string username, string password);
    Task<RadiusAuthResult> AuthenticateAsync(string identifier, string username, string password);
}

/// <summary>
/// RADIUS server configuration
/// </summary>
public class RadiusServerConfig
{
    public string Identifier { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 1812;
    public string Secret { get; set; } = string.Empty;
    public int Timeout { get; set; } = 5;
    public int Retries { get; set; } = 3;
    public string? Description { get; set; }
    public string? Dictionary { get; set; }
}

/// <summary>
/// RADIUS server info (for listing)
/// </summary>
public class RadiusServerInfo
{
    public int Id { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public int Timeout { get; set; }
    public int Retries { get; set; }
    public string? Description { get; set; }
    public string? Dictionary { get; set; }
}

/// <summary>
/// RADIUS test result
/// </summary>
public class RadiusTestResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// RADIUS authentication result
/// </summary>
public class RadiusAuthResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object>? Attributes { get; set; }
}
