// SPDX-FileCopyrightText: (C) 2025 Cornelius Kölbel <cornelius.koelbel@netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Info: https://privacyidea.org
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

using System.Text.RegularExpressions;

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Email validation utilities.
/// </summary>
public static partial class EmailValidation
{
    [GeneratedRegex(@"^[-\w\.]+@([\w-]+\.)+[\w-]{2,4}$")]
    private static partial Regex EmailRegex();

    /// <summary>
    /// Verify if the email is in valid format.
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return EmailRegex().IsMatch(email);
    }
}
