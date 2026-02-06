// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// API response formatting utilities.
/// </summary>
public static class ResponseFormatter
{
    /// <summary>
    /// Authentication response status values.
    /// </summary>
    public static class AuthResponse
    {
        public const string Accept = "ACCEPT";
        public const string Reject = "REJECT";
        public const string Challenge = "CHALLENGE";
        public const string Declined = "DECLINED";
    }

    /// <summary>
    /// Prepare a standardized API response envelope.
    /// </summary>
    /// <param name="value">Result value (can be dict, string, list, etc.)</param>
    /// <param name="rid">Request ID (for future versions)</param>
    /// <param name="details">Optional details dictionary</param>
    /// <param name="version">Version string</param>
    /// <param name="versionNumber">Version number</param>
    /// <returns>Response dictionary</returns>
    public static Dictionary<string, object> PrepareResult(
        object value,
        int rid = 1,
        Dictionary<string, object>? details = null,
        string version = "privacyIDEA 1.0.0",
        string versionNumber = "1.0.0")
    {
        var response = new Dictionary<string, object>
        {
            ["jsonrpc"] = "2.0",
            ["result"] = new Dictionary<string, object>
            {
                ["status"] = true,
                ["value"] = value
            },
            ["version"] = version,
            ["versionnumber"] = versionNumber,
            ["id"] = rid,
            ["time"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        if (details != null && details.Count > 0)
        {
            details["threadid"] = Environment.CurrentManagedThreadId;
            response["detail"] = details;
        }

        if (rid > 1)
        {
            var obj = value;
            if (value is Dictionary<string, object> dict)
            {
                obj = dict.GetValueOrDefault("auth");
            }

            string authentication;
            if (obj != null && !obj.Equals(AuthResponse.Challenge) && 
                (details == null || !details.ContainsKey("multi_challenge")))
            {
                authentication = AuthResponse.Accept;
            }
            else if (obj != null && obj.Equals(AuthResponse.Challenge))
            {
                authentication = AuthResponse.Challenge;
            }
            else if (details != null && 
                    (details.ContainsKey("multi_challenge") || details.ContainsKey("passkey")))
            {
                authentication = AuthResponse.Challenge;
            }
            else if (obj == null && details != null && 
                    details.GetValueOrDefault("challenge_status")?.Equals("declined") == true)
            {
                authentication = AuthResponse.Declined;
            }
            else
            {
                authentication = AuthResponse.Reject;
            }

            ((Dictionary<string, object>)response["result"])["authentication"] = authentication;
        }

        return response;
    }
}
