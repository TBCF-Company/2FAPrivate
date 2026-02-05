// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Network and IP address utilities for proxy handling and IP validation.
/// </summary>
public static class NetworkUtilities
{
    /// <summary>
    /// Parse the OverrideAuthorizationClient proxy settings string into a set of proxy paths.
    /// Valid strings are:
    /// - 10.0.0.0/24 > 192.168.0.0/24 (hosts in 10.0.0.x may specify clients as 192.168.0.x)
    /// - 10.0.0.1 > 192.168.0.0/24 > 192.168.1.0/24 (chained proxies)
    /// - 172.16.0.0/16 (hosts may rewrite to any client IP)
    /// Multiple settings may be separated by comma.
    /// </summary>
    /// <param name="proxySettings">The OverrideAuthorizationClient config string</param>
    /// <returns>Set of tuples of IP network ranges</returns>
    public static HashSet<List<IPNetwork>> ParseProxy(string proxySettings)
    {
        var proxySet = new HashSet<List<IPNetwork>>();

        if (string.IsNullOrWhiteSpace(proxySettings))
            return proxySet;

        var proxiesList = proxySettings.Split(',').Select(s => s.Trim());

        foreach (var proxy in proxiesList)
        {
            var pList = proxy.Split('>');
            List<IPNetwork> proxyPath;

            if (pList.Length > 1)
            {
                proxyPath = pList.Select(p => ParseIPNetwork(p.Trim())).ToList();
            }
            else
            {
                // No mapping client, so we take the whole network
                proxyPath = new List<IPNetwork>
                {
                    ParseIPNetwork(pList[0].Trim()),
                    ParseIPNetwork("0.0.0.0/0")
                };
            }

            proxySet.Add(proxyPath);
        }

        return proxySet;
    }

    /// <summary>
    /// Check proxy chain and determine the effective client IP.
    /// </summary>
    /// <param name="pathToClient">List of IP addresses from HTTP client to actual client</param>
    /// <param name="proxySettings">Proxy settings string</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>Effective client IP address</returns>
    public static IPAddress CheckProxy(List<IPAddress> pathToClient, string proxySettings, ILogger? logger = null)
    {
        HashSet<List<IPNetwork>> proxyDict;
        try
        {
            proxyDict = ParseProxy(proxySettings);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error parsing OverrideAuthorizationClient setting: {Settings}. The client IP will not be mapped!", proxySettings);
            return pathToClient[0];
        }

        logger?.LogDebug("Determining mapped IP from {Path} given proxy settings {Settings}...", pathToClient, proxySettings);
        
        int maxIdx = 0;

        foreach (var proxyPath in proxyDict)
        {
            logger?.LogDebug("Proxy path: {Path}", proxyPath);

            // If the proxy path contains more subnets than the path to the client, it cannot match
            if (proxyPath.Count > pathToClient.Count)
            {
                logger?.LogDebug("... ignored because it is longer than the path to the client");
                continue;
            }

            int currentMaxIdx = 0;

            for (int idx = 0; idx < Math.Min(proxyPath.Count, pathToClient.Count); idx++)
            {
                var proxyPathIp = proxyPath[idx];
                var clientPathIp = pathToClient[idx];

                if (!proxyPathIp.Contains(clientPathIp))
                {
                    logger?.LogDebug("... ignored because {ClientIp} is not in subnet {ProxyIp}", clientPathIp, proxyPathIp);
                    break;
                }
                
                currentMaxIdx = idx;

                // Check if this is the last iteration
                if (idx == Math.Min(proxyPath.Count, pathToClient.Count) - 1)
                {
                    if (currentMaxIdx >= maxIdx)
                    {
                        logger?.LogDebug("... setting new candidate for client IP: {Ip}", pathToClient[currentMaxIdx]);
                    }
                    maxIdx = Math.Max(maxIdx, currentMaxIdx);
                }
            }
        }

        logger?.LogDebug("Determined mapped client IP: {Ip}", pathToClient[maxIdx]);
        return pathToClient[maxIdx];
    }

    /// <summary>
    /// Get the client IP from an HTTP request, considering proxy settings.
    /// </summary>
    /// <param name="request">HTTP request</param>
    /// <param name="proxySettings">Proxy settings string</param>
    /// <param name="clientParam">Optional client parameter from request</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>Client IP address as string</returns>
    public static string? GetClientIp(HttpRequest request, string? proxySettings, string? clientParam = null, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(proxySettings))
        {
            return request.HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        var remoteAddr = request.HttpContext.Connection.RemoteIpAddress;
        if (remoteAddr == null)
            return null;

        var pathToClient = new List<IPAddress> { remoteAddr };

        // Check for X-Forwarded-For header
        if (request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var forwardedIps = forwardedFor.ToString()
                .Split(',')
                .Select(ip => ip.Trim())
                .Where(ip => IPAddress.TryParse(ip, out _))
                .Select(IPAddress.Parse)
                .Reverse();

            pathToClient.AddRange(forwardedIps);
        }

        // Add client parameter if provided
        if (!string.IsNullOrWhiteSpace(clientParam) && IPAddress.TryParse(clientParam, out var clientIp))
        {
            pathToClient.Add(clientIp);
        }

        return CheckProxy(pathToClient, proxySettings, logger).ToString();
    }

    /// <summary>
    /// Check if a client IP is contained in a policy list.
    /// The list can contain single IPs, subnets, and negated entries.
    /// Example: ["10.0.0.2", "192.168.2.1/24", "!192.168.2.12", "-172.16.200.1"]
    /// </summary>
    /// <param name="clientIp">The IP address to check</param>
    /// <param name="policy">List of IP addresses, subnets, and negated entries</param>
    /// <returns>Tuple of (found, excluded)</returns>
    public static (bool Found, bool Excluded) CheckIpInPolicy(string clientIp, List<string> policy)
    {
        bool clientFound = false;
        bool clientExcluded = false;

        var clientAddr = IPAddress.Parse(clientIp);

        // Remove empty strings from the list
        policy = policy.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

        foreach (var ipDef in policy)
        {
            if (ipDef.StartsWith('-') || ipDef.StartsWith('!'))
            {
                // Exclude the client?
                var network = ParseIPNetwork(ipDef[1..]);
                if (network.Contains(clientAddr))
                {
                    clientExcluded = true;
                }
            }
            else
            {
                var network = ParseIPNetwork(ipDef);
                if (network.Contains(clientAddr))
                {
                    clientFound = true;
                }
            }
        }

        return (clientFound, clientExcluded);
    }

    /// <summary>
    /// Parse an IP network string (e.g., "192.168.1.0/24" or "10.0.0.1").
    /// </summary>
    private static IPNetwork ParseIPNetwork(string networkString)
    {
        if (networkString.Contains('/'))
        {
            var parts = networkString.Split('/');
            var ip = IPAddress.Parse(parts[0]);
            var prefix = int.Parse(parts[1]);
            return new IPNetwork(ip, prefix);
        }
        else
        {
            var ip = IPAddress.Parse(networkString);
            var prefix = ip.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
            return new IPNetwork(ip, prefix);
        }
    }
}

/// <summary>
/// Represents an IP network with a prefix length.
/// </summary>
public class IPNetwork
{
    public IPAddress Network { get; }
    public int PrefixLength { get; }

    public IPNetwork(IPAddress network, int prefixLength)
    {
        Network = network;
        PrefixLength = prefixLength;
    }

    public bool Contains(IPAddress address)
    {
        if (Network.AddressFamily != address.AddressFamily)
            return false;

        var networkBytes = Network.GetAddressBytes();
        var addressBytes = address.GetAddressBytes();

        int bytesToCheck = PrefixLength / 8;
        int bitsToCheck = PrefixLength % 8;

        // Check full bytes
        for (int i = 0; i < bytesToCheck; i++)
        {
            if (networkBytes[i] != addressBytes[i])
                return false;
        }

        // Check remaining bits
        if (bitsToCheck > 0)
        {
            byte mask = (byte)(0xFF << (8 - bitsToCheck));
            if ((networkBytes[bytesToCheck] & mask) != (addressBytes[bytesToCheck] & mask))
                return false;
        }

        return true;
    }

    public override string ToString() => $"{Network}/{PrefixLength}";
}
