// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace XmlSigningExample.Api.Models;

/// <summary>
/// Response containing 2FA authentication code
/// </summary>
public class AuthCodeResponse
{
    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 2-character authentication code to be entered by user
    /// </summary>
    public string? AuthCode { get; set; }
    
    /// <summary>
    /// Session ID to track this signing request
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Message for user
    /// </summary>
    public string? Message { get; set; }
}
