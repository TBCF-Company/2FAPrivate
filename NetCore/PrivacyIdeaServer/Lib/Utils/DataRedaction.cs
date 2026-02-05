// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Data redaction utilities for censoring sensitive information.
/// </summary>
public static class DataRedaction
{
    /// <summary>
    /// Censor an email address from 'example.mail@test.com' to 'ex********@t****.com'.
    /// </summary>
    /// <param name="email">Email address to censor</param>
    /// <returns>Redacted email address</returns>
    public static string RedactedEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "**********@****.**";

        var parts = email.Split('@');
        var name = parts.Length > 0 ? parts[0] : "**";
        var domain = parts.Length > 1 ? parts[1] : "****.**";

        var nameRedacted = name.Length >= 2
            ? name[..2] + new string('*', 10 - Math.Min(2, name.Length))
            : new string('*', 10);

        var domainParts = domain.Split('.');
        var domainRedacted = domainParts.Length > 0
            ? $"{domainParts[0][0]}****.{domainParts[^1]}"
            : "****.**";

        return $"{nameRedacted}@{domainRedacted}";
    }

    /// <summary>
    /// Censor a phone number from '01234567890' to '****-******90'.
    /// </summary>
    /// <param name="number">Phone number to censor</param>
    /// <returns>Redacted phone number</returns>
    public static string RedactedPhoneNumber(string number)
    {
        if (string.IsNullOrWhiteSpace(number) || number.Length < 2)
            return "****-******";

        var lastTwo = number[^2..];
        return $"****-******{lastTwo}";
    }
}
