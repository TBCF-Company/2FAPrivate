// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

using QRCoder;

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// QR code generation utilities.
/// </summary>
public static class QrCodeGenerator
{
    /// <summary>
    /// Create a QR code PNG image data URI from input data.
    /// </summary>
    /// <param name="data">Input data to encode in the QR code</param>
    /// <param name="pixelsPerModule">Scaling of the final PNG image (default 10)</param>
    /// <returns>PNG data URI to be used in an &lt;img&gt; tag</returns>
    public static string CreateImg(string data, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        
        var pngBytes = qrCode.GetGraphic(pixelsPerModule);
        var base64 = Convert.ToBase64String(pngBytes);
        
        return $"data:image/png;base64,{base64}";
    }
}
