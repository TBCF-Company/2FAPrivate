namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Service for managing CA (Certificate Authority) connectors
/// </summary>
public interface ICAConnectorService
{
    /// <summary>
    /// Get all CA connectors
    /// </summary>
    Task<IEnumerable<CAConnectorInfo>> GetConnectorsAsync();

    /// <summary>
    /// Get a specific CA connector by name
    /// </summary>
    Task<CAConnectorInfo?> GetConnectorAsync(string name);

    /// <summary>
    /// Create or update a CA connector
    /// </summary>
    Task<CAConnectorInfo> CreateOrUpdateConnectorAsync(string name, string caType, Dictionary<string, string> config);

    /// <summary>
    /// Delete a CA connector
    /// </summary>
    Task<bool> DeleteConnectorAsync(string name);

    /// <summary>
    /// Get CA certificates from a connector
    /// </summary>
    Task<IEnumerable<CertificateInfo>> GetCACertificatesAsync(string connectorName);

    /// <summary>
    /// Request a certificate from CA
    /// </summary>
    Task<CertificateRequestResult> RequestCertificateAsync(string connectorName, CertificateRequest request);

    /// <summary>
    /// Revoke a certificate
    /// </summary>
    Task<bool> RevokeCertificateAsync(string connectorName, string serialNumber, string reason);

    /// <summary>
    /// Get available CA connector types
    /// </summary>
    IEnumerable<string> GetAvailableTypes();
}

public class CAConnectorInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CAType { get; set; } = string.Empty;
    public Dictionary<string, string> Config { get; set; } = new();
}

public class CertificateInfo
{
    public string SerialNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
    public bool IsRevoked { get; set; }
}

public class CertificateRequest
{
    public string CommonName { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string? OrganizationalUnit { get; set; }
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? Locality { get; set; }
    public string? Email { get; set; }
    public string? CSR { get; set; }
    public int ValidityDays { get; set; } = 365;
}

public class CertificateRequestResult
{
    public bool Success { get; set; }
    public string? Certificate { get; set; }
    public string? SerialNumber { get; set; }
    public string? ErrorMessage { get; set; }
}
