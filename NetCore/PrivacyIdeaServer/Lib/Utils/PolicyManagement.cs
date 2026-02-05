// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Policy management utilities.
/// </summary>
public static class PolicyManagement
{
    /// <summary>
    /// Reduce the realm list based on policies.
    /// If there is a policy that acts for all realms, all realms are returned.
    /// Otherwise, only realms contained in the policies are returned.
    /// </summary>
    /// <param name="allRealms">All available realms</param>
    /// <param name="policies">Active policies</param>
    /// <returns>Filtered realm dictionary</returns>
    public static Dictionary<string, object> ReduceRealms(
        Dictionary<string, object> allRealms,
        List<Dictionary<string, object>>? policies)
    {
        var realms = new Dictionary<string, object>();

        if (policies == null || policies.Count == 0)
        {
            // If no policies, works for all realms
            return allRealms;
        }

        foreach (var policy in policies)
        {
            if (!policy.TryGetValue("realm", out var realmValue) || realmValue == null)
            {
                // Empty realm means policy acts for ALL realms
                return allRealms;
            }

            if (realmValue is List<string> policyRealms)
            {
                foreach (var r in policyRealms)
                {
                    if (!realms.ContainsKey(r) && allRealms.ContainsKey(r))
                    {
                        realms[r] = allRealms[r];
                    }
                }
            }
        }

        return realms;
    }
}
