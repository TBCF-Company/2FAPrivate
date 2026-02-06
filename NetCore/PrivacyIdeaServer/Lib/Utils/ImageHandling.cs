// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Image file handling utilities.
/// </summary>
public static class ImageHandling
{
    /// <summary>
    /// Read an image file and convert it to a data URI string for use in HTML.
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>Data URI string (data:image/png;base64,...)</returns>
    public static string ConvertImageFileToDataImage(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                return string.Empty;
            }

            var extension = Path.GetExtension(imagePath).ToLowerInvariant();
            var mimeType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };

            var bytes = File.ReadAllBytes(imagePath);
            var base64 = Convert.ToBase64String(bytes);

            return $"data:{mimeType};base64,{base64}";
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
