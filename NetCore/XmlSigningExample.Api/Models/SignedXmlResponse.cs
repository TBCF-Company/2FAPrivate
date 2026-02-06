// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace XmlSigningExample.Api.Models;

/// <summary>
/// Response containing signed XML document
/// </summary>
public class SignedXmlResponse
{
    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Signed XML content
    /// </summary>
    public string? SignedXml { get; set; }
    
    /// <summary>
    /// Message for user
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// Timestamp when the XML was signed
    /// </summary>
    public DateTime? SignedAt { get; set; }
}
