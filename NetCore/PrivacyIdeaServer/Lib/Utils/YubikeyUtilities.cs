// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

using System.Text;

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Yubikey-specific utilities for Modhex encoding/decoding and CRC-16 checksums.
/// </summary>
public static class YubikeyUtilities
{
    private const string HexHexChars = "0123456789abcdef";
    private const string ModHexChars = "cbdefghijklnrtuv";

    private static readonly Dictionary<char, char> Hex2ModDict;
    private static readonly Dictionary<char, char> Mod2HexDict;

    static YubikeyUtilities()
    {
        Hex2ModDict = HexHexChars.Zip(ModHexChars, (h, m) => new { h, m })
            .ToDictionary(x => x.h, x => x.m);
        Mod2HexDict = ModHexChars.Zip(HexHexChars, (m, h) => new { m, h })
            .ToDictionary(x => x.m, x => x.h);
    }

    /// <summary>
    /// Encode a string or byte array to Yubikey Modhex format.
    /// </summary>
    /// <param name="value">String or byte array to encode</param>
    /// <returns>Modhex encoded string</returns>
    public static string ModhexEncode(object value)
    {
        var hexString = StringEncoding.HexlifyAndUnicode(value);
        var result = new StringBuilder(hexString.Length);

        foreach (char c in hexString)
        {
            result.Append(Hex2ModDict[c]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Decode a Modhex string to byte array.
    /// </summary>
    /// <param name="modhexString">Modhex encoded string</param>
    /// <returns>Decoded byte array</returns>
    public static byte[] ModhexDecode(string modhexString)
    {
        var hexString = new StringBuilder(modhexString.Length);

        foreach (char c in modhexString)
        {
            if (!Mod2HexDict.TryGetValue(c, out var hexChar))
                throw new ArgumentException($"Invalid Modhex character: {c}");
            
            hexString.Append(hexChar);
        }

        return Convert.FromHexString(hexString.ToString());
    }

    /// <summary>
    /// Calculate CRC-16 (16-bit ISO 13239 1st complement) checksum.
    /// (see Yubikey-Manual - Chapter 6: Implementation details)
    /// </summary>
    /// <param name="data">Input byte array for CRC calculation</param>
    /// <returns>CRC-16 checksum</returns>
    public static ushort Checksum(byte[] data)
    {
        ushort crc = 0xffff;

        foreach (byte b in data)
        {
            crc ^= (ushort)(b & 0xff);

            for (int j = 0; j < 8; j++)
            {
                int n = crc & 1;
                crc >>= 1;
                if (n != 0)
                {
                    crc ^= 0x8408;
                }
            }
        }

        return crc;
    }
}
