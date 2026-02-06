// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

using System.Web;
using Microsoft.AspNetCore.Http;

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Token and authentication related utilities.
/// </summary>
public static class TokenUtilities
{
    /// <summary>
    /// Split a password into PIN and OTP value based on OTP length and prepend setting.
    /// </summary>
    /// <param name="password">The password like "test123456" or "123456test"</param>
    /// <param name="otpLen">The length of the OTP value</param>
    /// <param name="prependPin">Whether the PIN is prepended (true) or appended (false)</param>
    /// <returns>Tuple of (pin, otpValue)</returns>
    public static (string Pin, string OtpValue) SplitPinPass(string password, int otpLen, bool prependPin)
    {
        if (prependPin)
        {
            var pin = password.Length > otpLen ? password[..^otpLen] : string.Empty;
            var otpVal = password.Length >= otpLen ? password[^otpLen..] : password;
            return (pin, otpVal);
        }
        else
        {
            var pin = password.Length > otpLen ? password[otpLen..] : string.Empty;
            var otpVal = password.Length >= otpLen ? password[..otpLen] : password;
            return (pin, otpVal);
        }
    }

    /// <summary>
    /// Create a tag dictionary for email templates and notification handlers.
    /// </summary>
    /// <param name="loggedInUser">The acting logged-in user (admin)</param>
    /// <param name="request">The HTTP request object</param>
    /// <param name="serial">Token serial number</param>
    /// <param name="tokenOwner">Token owner user object</param>
    /// <param name="tokenType">Type of the token</param>
    /// <param name="tokenDescription">Description of the token</param>
    /// <param name="recipient">Recipient dictionary</param>
    /// <param name="registrationCode">Registration code</param>
    /// <param name="googleUrlValue">URL for QR code during enrollment</param>
    /// <param name="googleUrlImg">Image data blob of QR code</param>
    /// <param name="pushUrlValue">URL for Push-Token enrollment</param>
    /// <param name="pushUrlImg">Image data of Push-Token QR code</param>
    /// <param name="clientIp">Client IP address</param>
    /// <param name="pin">Token PIN</param>
    /// <param name="challenge">Challenge data</param>
    /// <param name="escapeHtml">Whether to HTML escape values</param>
    /// <param name="containerSerial">Container serial number</param>
    /// <param name="containerUrlValue">Container registration URL</param>
    /// <param name="containerUrlImg">Container QR code URL</param>
    /// <returns>Tag dictionary</returns>
    public static Dictionary<string, string?> CreateTagDict(
        Dictionary<string, string>? loggedInUser = null,
        HttpRequest? request = null,
        string? serial = null,
        TokenOwner? tokenOwner = null,
        string? tokenType = null,
        string? tokenDescription = null,
        Dictionary<string, string>? recipient = null,
        string? registrationCode = null,
        string? googleUrlValue = null,
        string? googleUrlImg = null,
        string? pushUrlValue = null,
        string? pushUrlImg = null,
        string? clientIp = null,
        string? pin = null,
        string? challenge = null,
        bool escapeHtml = false,
        string? containerSerial = null,
        string? containerUrlValue = null,
        string? containerUrlImg = null)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        recipient ??= new Dictionary<string, string>();

        var tags = new Dictionary<string, string?>
        {
            ["admin"] = loggedInUser?.GetValueOrDefault("username"),
            ["realm"] = loggedInUser?.GetValueOrDefault("realm"),
            ["action"] = request?.Path.Value,
            ["serial"] = serial,
            ["url"] = request != null ? $"{request.Scheme}://{request.Host}" : null,
            ["user"] = tokenOwner?.GivenName,
            ["surname"] = tokenOwner?.Surname,
            ["givenname"] = tokenOwner?.GivenName,
            ["username"] = tokenOwner?.Login,
            ["userrealm"] = tokenOwner?.Realm,
            ["tokentype"] = tokenType,
            ["tokendescription"] = tokenDescription,
            ["registrationcode"] = registrationCode,
            ["recipient_givenname"] = recipient.GetValueOrDefault("givenname"),
            ["recipient_surname"] = recipient.GetValueOrDefault("surname"),
            ["googleurl_value"] = googleUrlValue,
            ["googleurl_img"] = googleUrlImg,
            ["pushurl_value"] = pushUrlValue,
            ["pushurl_img"] = pushUrlImg,
            ["time"] = time,
            ["date"] = date,
            ["client_ip"] = clientIp,
            ["pin"] = pin,
            ["ua_browser"] = request?.Headers.UserAgent.ToString(),
            ["ua_string"] = request?.Headers.UserAgent.ToString(),
            ["challenge"] = challenge ?? string.Empty,
            ["container_serial"] = containerSerial,
            ["container_url_value"] = containerUrlValue,
            ["container_url_img"] = containerUrlImg
        };

        if (escapeHtml)
        {
            var escapedTags = new Dictionary<string, string?>();
            foreach (var kvp in tags)
            {
                escapedTags[kvp.Key] = kvp.Value != null ? HttpUtility.HtmlEncode(kvp.Value) : null;
            }
            return escapedTags;
        }

        return tags;
    }

    /// <summary>
    /// Determine logged-in user parameters for admin vs normal user.
    /// </summary>
    /// <param name="loggedInUser">Logged-in user dictionary</param>
    /// <param name="params">Request parameters</param>
    /// <returns>Tuple of (role, username, realm, adminUser, adminRealm)</returns>
    public static (string Role, string? Username, string? Realm, string? AdminUser, string? AdminRealm)
        DetermineLoggedInUserParams(Dictionary<string, string> loggedInUser, Dictionary<string, string> @params)
    {
        var role = loggedInUser.GetValueOrDefault("role") ?? string.Empty;
        var username = loggedInUser.GetValueOrDefault("username");
        var realm = loggedInUser.GetValueOrDefault("realm");
        string? adminRealm = null;
        string? adminUser = null;

        if (role == "admin")
        {
            adminRealm = realm;
            adminUser = username;
            username = @params.GetValueOrDefault("user");
            realm = @params.GetValueOrDefault("realm");
        }
        else if (role != "user")
        {
            throw new ArgumentException($"Unknown role: {role}");
        }

        return (role, username, realm, adminUser, adminRealm);
    }
}

/// <summary>
/// Token owner information.
/// </summary>
/// <param name="Login">User login name</param>
/// <param name="Realm">User realm</param>
/// <param name="GivenName">User given name</param>
/// <param name="Surname">User surname</param>
public record TokenOwner(string Login, string Realm, string? GivenName = null, string? Surname = null);
