// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace XmlSigningExample.Api.Models;

/// <summary>
/// Request to verify 2FA code and complete XML signing
/// </summary>
public class VerifyAndSignRequest
{
    /// <summary>
    /// Session ID from the initial signing request
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 2-character code entered by user from authenticator app
    /// </summary>
    public string AuthCode { get; set; } = string.Empty;
}
