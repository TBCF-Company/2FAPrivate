using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// Machine service implementation
/// Maps to Python: privacyidea/lib/machine.py
/// </summary>
public class MachineService : IMachineService
{
    private readonly ILogger<MachineService> _logger;
    private readonly Dictionary<string, MachineInfo> _machines = new();
    private readonly List<MachineTokenInfo> _machineTokens = new();
    private int _nextId = 1;

    public MachineService(ILogger<MachineService> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<MachineInfo>> GetMachinesAsync(string? hostname = null)
    {
        IEnumerable<MachineInfo> result = _machines.Values;
        
        if (!string.IsNullOrEmpty(hostname))
        {
            result = result.Where(m => m.Hostname.Contains(hostname, StringComparison.OrdinalIgnoreCase));
        }
        
        return Task.FromResult(result);
    }

    public Task<int> CreateMachineAsync(MachineInfo machine)
    {
        machine.Id = _nextId++;
        _machines[machine.Hostname] = machine;
        _logger.LogInformation("Created machine {Hostname}", machine.Hostname);
        return Task.FromResult(machine.Id);
    }

    public Task<bool> DeleteMachineAsync(string hostname)
    {
        var result = _machines.Remove(hostname);
        if (result)
        {
            _machineTokens.RemoveAll(mt => mt.Hostname == hostname);
            _logger.LogInformation("Deleted machine {Hostname}", hostname);
        }
        return Task.FromResult(result);
    }

    public Task<bool> AttachTokenAsync(string hostname, string serial, string application, Dictionary<string, string>? options = null)
    {
        var existing = _machineTokens.FirstOrDefault(mt => 
            mt.Hostname == hostname && mt.Serial == serial && mt.Application == application);
        
        if (existing != null)
        {
            existing.Options = options ?? new Dictionary<string, string>();
        }
        else
        {
            _machineTokens.Add(new MachineTokenInfo
            {
                Hostname = hostname,
                Serial = serial,
                Application = application,
                Options = options ?? new Dictionary<string, string>()
            });
        }
        
        _logger.LogInformation("Attached token {Serial} to machine {Hostname} for {Application}", 
            serial, hostname, application);
        return Task.FromResult(true);
    }

    public Task<bool> DetachTokenAsync(string hostname, string serial, string application)
    {
        var removed = _machineTokens.RemoveAll(mt => 
            mt.Hostname == hostname && mt.Serial == serial && mt.Application == application);
        
        if (removed > 0)
        {
            _logger.LogInformation("Detached token {Serial} from machine {Hostname}", serial, hostname);
        }
        
        return Task.FromResult(removed > 0);
    }

    public Task<IEnumerable<MachineTokenInfo>> GetMachineTokensAsync(string? hostname = null, string? serial = null)
    {
        IEnumerable<MachineTokenInfo> result = _machineTokens;
        
        if (!string.IsNullOrEmpty(hostname))
        {
            result = result.Where(mt => mt.Hostname == hostname);
        }
        
        if (!string.IsNullOrEmpty(serial))
        {
            result = result.Where(mt => mt.Serial == serial);
        }
        
        return Task.FromResult(result);
    }

    public Task<IEnumerable<AuthItemInfo>> GetAuthItemsAsync(string application, string? hostname = null)
    {
        // For SSH, this would return SSH public keys
        // For LUKS, this would return LUKS keys
        // For offline, this would return offline OTP values
        
        var tokens = _machineTokens.Where(mt => mt.Application == application);
        
        if (!string.IsNullOrEmpty(hostname))
        {
            tokens = tokens.Where(mt => mt.Hostname == hostname);
        }
        
        var result = tokens.Select(mt => new AuthItemInfo
        {
            Hostname = mt.Hostname,
            Serial = mt.Serial,
            Type = application,
            Data = mt.Options.ToDictionary(k => k.Key, v => (object)v.Value)
        });
        
        return Task.FromResult(result);
    }
}
