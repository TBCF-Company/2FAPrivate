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
/// String encoding and conversion utilities for converting between different encodings
/// and formats including UTF-8, Base32, Base64, and hexadecimal.
/// </summary>
public static class StringEncoding
{
    /// <summary>
    /// Convert a string to UTF-8 encoded bytes.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>UTF-8 encoded byte array, or null if input is null</returns>
    public static byte[]? ToUtf8(string? value)
    {
        return value == null ? null : Encoding.UTF8.GetBytes(value);
    }

    /// <summary>
    /// Convert bytes or string to UTF-8 string.
    /// </summary>
    /// <param name="value">Byte array or string to convert</param>
    /// <param name="encoding">The encoding to use for decoding (default UTF-8)</param>
    /// <returns>UTF-8 string representation</returns>
    public static string ToUnicode(object? value, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        return value switch
        {
            null => string.Empty,
            string str => str,
            byte[] bytes => encoding.GetString(bytes),
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Convert a string to a byte array using UTF-8 encoding.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>UTF-8 encoded byte array</returns>
    public static byte[] ToBytes(object? value)
    {
        return value switch
        {
            null => Array.Empty<byte>(),
            byte[] bytes => bytes,
            string str => Encoding.UTF8.GetBytes(str),
            _ => Encoding.UTF8.GetBytes(value.ToString() ?? string.Empty)
        };
    }

    /// <summary>
    /// Convert a value to a byte string. If it is not a string type, convert it to a string first.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>Byte string representing the value</returns>
    public static byte[] ToByteString(object? value)
    {
        if (value is byte[] bytes)
            return bytes;

        if (value is not string str)
            str = value?.ToString() ?? string.Empty;

        return ToBytes(str);
    }

    /// <summary>
    /// Hexlify a string or byte array and return as Unicode string.
    /// </summary>
    /// <param name="value">String or byte array to hexlify</param>
    /// <returns>Hexadecimal string representation</returns>
    public static string HexlifyAndUnicode(object value)
    {
        var bytes = ToBytes(value);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Base32-encode a string or byte array and return as Unicode string.
    /// </summary>
    /// <param name="value">String or byte array to encode</param>
    /// <returns>Base32-encoded string</returns>
    public static string B32EncodeAndUnicode(object value)
    {
        var bytes = ToBytes(value);
        return Base32Encode(bytes);
    }

    /// <summary>
    /// Base64-encode a string or byte array and return as Unicode string.
    /// </summary>
    /// <param name="value">String or byte array to encode</param>
    /// <returns>Base64-encoded string</returns>
    public static string B64EncodeAndUnicode(object value)
    {
        var bytes = ToBytes(value);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Base64-urlsafe-encode a string or byte array and return as Unicode string.
    /// </summary>
    /// <param name="value">String or byte array to encode</param>
    /// <returns>URL-safe Base64-encoded string</returns>
    public static string UrlSafeB64EncodeAndUnicode(object value)
    {
        var bytes = ToBytes(value);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Convert a database column value to Unicode string.
    /// </summary>
    /// <param name="value">The column value to convert</param>
    /// <returns>Unicode string or null</returns>
    public static string? ConvertColumnToUnicode(object? value)
    {
        return value switch
        {
            null => null,
            string str => str,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Base32 encoding implementation (RFC 4648).
    /// </summary>
    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder((data.Length + 4) / 5 * 8);
        
        for (int i = 0; i < data.Length; i += 5)
        {
            int byteCount = Math.Min(5, data.Length - i);
            ulong buffer = 0;

            for (int j = 0; j < byteCount; j++)
            {
                buffer = (buffer << 8) | data[i + j];
            }

            int bitCount = byteCount * 8;
            buffer <<= (40 - bitCount);

            for (int j = 0; j < (bitCount + 4) / 5; j++)
            {
                int index = (int)((buffer >> (35 - j * 5)) & 0x1F);
                result.Append(alphabet[index]);
            }
        }

        // Add padding
        while (result.Length % 8 != 0)
        {
            result.Append('=');
        }

        return result.ToString();
    }

    /// <summary>
    /// Base32 decoding implementation (RFC 4648).
    /// </summary>
    public static byte[] Base32Decode(string encoded)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        encoded = encoded.TrimEnd('=').ToUpperInvariant();
        
        var result = new List<byte>();
        ulong buffer = 0;
        int bitsLeft = 0;

        foreach (char c in encoded)
        {
            int value = alphabet.IndexOf(c);
            if (value < 0)
                throw new ArgumentException($"Invalid Base32 character: {c}");

            buffer = (buffer << 5) | (ulong)value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                result.Add((byte)(buffer >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }

        return result.ToArray();
    }
}
