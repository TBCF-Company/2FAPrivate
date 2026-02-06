// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace XmlSigningExample.Api.Models;

/// <summary>
/// Request to sign an XML document
/// </summary>
public class XmlSigningRequest
{
    /// <summary>
    /// The XML content to be signed
    /// </summary>
    public string XmlContent { get; set; } = string.Empty;
    
    /// <summary>
    /// Username requesting the signature
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
