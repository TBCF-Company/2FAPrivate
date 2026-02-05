// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Validation helper utilities for names, serials, PINs, and data formats.
/// </summary>
public static partial class ValidationHelpers
{
    private const string AllowedSerial = @"^[0-9a-zA-Z\-_]+$";
    private const string DefaultNameExpression = @"^[A-Za-z0-9_\-\.]+$";

    private static readonly Dictionary<char, string> CharListContentPolicy = new()
    {
        { 'c', "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" },  // characters
        { 'n', "0123456789" },  // numbers
        { 's', "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~" }  // special (punctuation)
    };

    /// <summary>
    /// Check if a name conforms to the given regular expression pattern.
    /// </summary>
    /// <param name="name">The name to check</param>
    /// <param name="nameExpression">Regular expression pattern (default: alphanumeric, dash, underscore, dot)</param>
    /// <returns>True if valid</returns>
    /// <exception cref="ArgumentException">Thrown if name contains non-conformant characters</exception>
    public static bool SanityNameCheck(string name, string nameExpression = DefaultNameExpression)
    {
        if (!Regex.IsMatch(name, nameExpression))
        {
            throw new ArgumentException($"Non conformant characters in the name: {name} (not in {nameExpression})");
        }
        return true;
    }

    /// <summary>
    /// Check if a serial number is valid.
    /// </summary>
    /// <param name="serial">The serial number to check</param>
    /// <returns>True if valid</returns>
    /// <exception cref="ArgumentException">Thrown if serial format is invalid</exception>
    public static bool CheckSerialValid(string serial)
    {
        if (!Regex.IsMatch(serial, AllowedSerial))
        {
            throw new ArgumentException($"Invalid serial number. Must comply to {AllowedSerial}.");
        }
        return true;
    }

    /// <summary>
    /// Decode arbitrary data given in base32check format:
    /// strip_padding(base32(sha1(payload)[:4] + payload))
    /// </summary>
    /// <param name="encodedData">The base32 encoded data</param>
    /// <param name="alwaysUpper">If lowercase should be converted to uppercase</param>
    /// <returns>Hex-encoded payload</returns>
    /// <exception cref="ArgumentException">Thrown if data is malformed</exception>
    public static string DecodeBase32Check(string encodedData, bool alwaysUpper = true)
    {
        // Add padding to have a multiple of 8 bytes
        if (alwaysUpper)
            encodedData = encodedData.ToUpperInvariant();

        int encodedLength = encodedData.Length;
        if (encodedLength % 8 != 0)
        {
            encodedData += new string('=', 8 - (encodedLength % 8));
        }

        // Decode as base32
        byte[] decodedData;
        try
        {
            decodedData = StringEncoding.Base32Decode(encodedData);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Malformed base32check data: Invalid base32", ex);
        }

        // Extract checksum and payload
        if (decodedData.Length < 4)
        {
            throw new ArgumentException("Malformed base32check data: Too short");
        }

        var checksum = decodedData[..4];
        var payload = decodedData[4..];

        // Verify checksum using SHA1
        var payloadHash = SHA1.HashData(payload);
        if (!checksum.SequenceEqual(payloadHash[..4]))
        {
            throw new ArgumentException("Malformed base32check data: Incorrect checksum");
        }

        return StringEncoding.HexlifyAndUnicode(payload);
    }

    /// <summary>
    /// Generate character lists from PIN policy string.
    /// </summary>
    /// <param name="policy">The policy string (e.g., "+cns", "-ns", "[asdf]")</param>
    /// <returns>Dictionary with "base" characters and "requirements" list</returns>
    public static PinPolicyResult GenerateCharListsFromPinPolicy(string policy)
    {
        var validPolicyRegex = new Regex(@"^[+-]*[cns]+$|^\[.*\]+$");

        // Default: full character list
        var baseCharacters = string.Join("", CharListContentPolicy.Values);
        var requirements = new List<string>();

        if (!validPolicyRegex.IsMatch(policy))
        {
            throw new ArgumentException("Unknown character specifier in PIN policy.");
        }

        if (policy.StartsWith('+'))
        {
            // Grouping
            var charList = new StringBuilder();
            foreach (char c in policy[1..])
            {
                if (CharListContentPolicy.TryGetValue(c, out var chars))
                    charList.Append(chars);
            }
            requirements.Add(charList.ToString());
        }
        else if (policy.StartsWith('-'))
        {
            // Exclusion
            var baseCharList = new StringBuilder();
            foreach (var kvp in CharListContentPolicy)
            {
                if (!policy[1..].Contains(kvp.Key))
                {
                    baseCharList.Append(kvp.Value);
                }
            }
            baseCharacters = baseCharList.ToString();
        }
        else if (policy.StartsWith('[') && policy.EndsWith(']'))
        {
            // Only allowed characters
            baseCharacters = policy[1..^1];
        }
        else
        {
            // Individual requirements
            foreach (char c in policy)
            {
                if (CharListContentPolicy.TryGetValue(c, out var chars))
                {
                    requirements.Add(chars);
                }
            }
        }

        return new PinPolicyResult(baseCharacters, requirements);
    }

    /// <summary>
    /// Check if a PIN conforms to the given policy.
    /// </summary>
    /// <param name="pin">The PIN to check</param>
    /// <param name="policy">The policy string</param>
    /// <returns>Tuple of (valid, error message)</returns>
    public static (bool Valid, string Message) CheckPinContents(string pin, string policy)
    {
        if (string.IsNullOrEmpty(policy))
        {
            return (false, "No policy given.");
        }

        var comments = new List<string>();
        bool valid = true;

        var charLists = GenerateCharListsFromPinPolicy(policy);

        // Check for not allowed characters
        foreach (char c in pin)
        {
            if (!charLists.BaseCharacters.Contains(c))
            {
                valid = false;
                break;
            }
        }

        if (!valid)
        {
            comments.Add("Not allowed character in PIN!");
        }

        // Check requirements
        foreach (var requiredChars in charLists.Requirements)
        {
            if (!pin.Any(c => requiredChars.Contains(c)))
            {
                valid = false;
                comments.Add($"Missing character in PIN: {requiredChars}");
            }
        }

        return (valid, string.Join(", ", comments));
    }
}

/// <summary>
/// Result of PIN policy parsing.
/// </summary>
/// <param name="BaseCharacters">Base set of allowed characters</param>
/// <param name="Requirements">List of character sets, at least one character from each is required</param>
public record PinPolicyResult(string BaseCharacters, List<string> Requirements);
