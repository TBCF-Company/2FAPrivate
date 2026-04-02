using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// Service for managing CA connectors
/// </summary>
public class CAConnectorService : ICAConnectorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CAConnectorService> _logger;

    private static readonly string[] AvailableCATypes = { "local", "microsoft", "openssl" };

    public CAConnectorService(IUnitOfWork unitOfWork, ILogger<CAConnectorService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<CAConnectorInfo>> GetConnectorsAsync()
    {
        var connectors = await _unitOfWork.Query<CAConnector>()
            .Include(c => c.Configs)
            .ToListAsync();

        return connectors.Select(MapToInfo);
    }

    public async Task<CAConnectorInfo?> GetConnectorAsync(string name)
    {
        var connector = await _unitOfWork.Query<CAConnector>()
            .Include(c => c.Configs)
            .FirstOrDefaultAsync(c => c.Name == name);

        return connector != null ? MapToInfo(connector) : null;
    }

    public async Task<CAConnectorInfo> CreateOrUpdateConnectorAsync(string name, string caType, Dictionary<string, string> config)
    {
        var existing = await _unitOfWork.Query<CAConnector>()
            .Include(c => c.Configs)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (existing != null)
        {
            existing.CAType = caType;
            
            // Clear existing configs
            foreach (var cfg in existing.Configs.ToList())
            {
                _unitOfWork.Delete(cfg);
            }
            existing.Configs.Clear();

            // Add new configs
            foreach (var kvp in config)
            {
                existing.Configs.Add(new CAConnectorConfig
                {
                    CAConnectorId = existing.Id,
                    Key = kvp.Key,
                    Value = kvp.Value
                });
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Updated CA connector: {Name}", name);
            return MapToInfo(existing);
        }

        var connector = new CAConnector
        {
            Name = name,
            CAType = caType
        };

        foreach (var kvp in config)
        {
            connector.Configs.Add(new CAConnectorConfig
            {
                Key = kvp.Key,
                Value = kvp.Value
            });
        }

        _unitOfWork.Add(connector);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Created CA connector: {Name}", name);
        return MapToInfo(connector);
    }

    public async Task<bool> DeleteConnectorAsync(string name)
    {
        var connector = await _unitOfWork.Query<CAConnector>()
            .FirstOrDefaultAsync(c => c.Name == name);

        if (connector == null)
            return false;

        _unitOfWork.Delete(connector);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Deleted CA connector: {Name}", name);
        return true;
    }

    public async Task<IEnumerable<CertificateInfo>> GetCACertificatesAsync(string connectorName)
    {
        var connector = await GetConnectorAsync(connectorName);
        if (connector == null)
            return Array.Empty<CertificateInfo>();

        // TODO: Implement actual CA certificate retrieval based on connector type
        _logger.LogWarning("GetCACertificates not fully implemented for connector: {Name}", connectorName);
        return Array.Empty<CertificateInfo>();
    }

    public async Task<CertificateRequestResult> RequestCertificateAsync(string connectorName, CertificateRequest request)
    {
        var connector = await GetConnectorAsync(connectorName);
        if (connector == null)
        {
            return new CertificateRequestResult
            {
                Success = false,
                ErrorMessage = $"CA connector '{connectorName}' not found"
            };
        }

        // TODO: Implement actual certificate request based on connector type
        _logger.LogWarning("RequestCertificate not fully implemented for connector: {Name}", connectorName);
        
        return new CertificateRequestResult
        {
            Success = false,
            ErrorMessage = "Certificate request not implemented"
        };
    }

    public async Task<bool> RevokeCertificateAsync(string connectorName, string serialNumber, string reason)
    {
        var connector = await GetConnectorAsync(connectorName);
        if (connector == null)
            return false;

        // TODO: Implement actual certificate revocation
        _logger.LogWarning("RevokeCertificate not fully implemented for connector: {Name}", connectorName);
        return false;
    }

    public IEnumerable<string> GetAvailableTypes() => AvailableCATypes;

    private static CAConnectorInfo MapToInfo(CAConnector connector)
    {
        return new CAConnectorInfo
        {
            Id = connector.Id,
            Name = connector.Name,
            CAType = connector.CAType,
            Config = connector.Configs.ToDictionary(c => c.Key, c => c.Value ?? string.Empty)
        };
    }
}
