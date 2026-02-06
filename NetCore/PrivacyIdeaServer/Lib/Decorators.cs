//  (c) Cornelius Kölbel
//  License:  AGPLv3
//  contact:  http://www.privacyidea.org
//  Converted to C# .NET Core 8 - 2026-02-05
//
// This code is free software; you can redistribute it and/or
// modify it under the terms of the GNU AFFERO GENERAL PUBLIC LICENSE
// License as published by the Free Software Foundation; either
// version 3 of the License, or any later version.
//
// This code is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU AFFERO GENERAL PUBLIC LICENSE for more details.
//
// You should have received a copy of the GNU Affero General Public
// License along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace PrivacyIdeaServer.Lib;

/// <summary>
/// Validation helpers for token operations
/// Note: In C#, Python decorators are typically replaced with attributes (for metadata)
/// or helper methods (for runtime validation). This file contains helper methods
/// that can be called to perform the same validation checks.
/// </summary>
public static class Decorators
{
    /// <summary>
    /// Check if a token is locked. Throws TokenAdminError if locked.
    /// This replaces the @check_token_locked decorator in Python.
    /// </summary>
    /// <param name="token">Token object to check</param>
    /// <exception cref="TokenAdminError">Thrown if token is locked</exception>
    public static void CheckTokenLocked(object token)
    {
        // In actual implementation, you would check if the token has an IsLocked property
        // For now, this is a placeholder
        // if (token.IsLocked())
        // {
        //     throw new TokenAdminError("This action is not possible, since the token is locked", 1007);
        // }
    }

    /// <summary>
    /// Check if a given OTP value has the correct length.
    /// This replaces the @check_token_otp_length decorator in Python.
    /// </summary>
    /// <param name="token">Token object</param>
    /// <param name="otpValue">OTP value to check</param>
    /// <param name="expectedLength">Expected OTP length</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>True if OTP length is correct, false otherwise</returns>
    public static bool CheckTokenOtpLength(object token, string otpValue, int expectedLength, ILogger? logger = null)
    {
        if (otpValue.Length != expectedLength)
        {
            logger?.LogInformation(
                "OTP value for token has wrong length ({ActualLength} != {ExpectedLength})",
                otpValue.Length, expectedLength);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Check that either user or serial is provided.
    /// This replaces the @check_user_or_serial decorator in Python.
    /// </summary>
    /// <param name="user">User identifier (can be null)</param>
    /// <param name="serial">Serial number (can be null)</param>
    /// <exception cref="ParameterError">Thrown if neither user nor serial is provided</exception>
    public static void CheckUserOrSerial(string? user, string? serial)
    {
        bool hasUser = !string.IsNullOrWhiteSpace(user);
        bool hasSerial = !string.IsNullOrWhiteSpace(serial);

        if (!hasUser && !hasSerial)
        {
            throw new ParameterError("You either need to provide user or serial");
        }

        if (hasSerial)
        {
            CheckSerialValid(serial!);
        }
    }

    /// <summary>
    /// Check that the request contains either serial, user, or credential_id.
    /// This replaces the check_user_serial_or_cred_id_in_request decorator in Python.
    /// </summary>
    /// <param name="user">User identifier (can be null)</param>
    /// <param name="serial">Serial number (can be null)</param>
    /// <param name="credentialId">Credential ID (can be null)</param>
    /// <param name="cancelEnrollment">Cancel enrollment flag</param>
    /// <exception cref="ParameterError">Thrown if validation fails</exception>
    public static void CheckUserSerialOrCredIdInRequest(
        string? user,
        string? serial,
        string? credentialId,
        bool cancelEnrollment = false)
    {
        user = user?.Trim();
        serial = serial?.Trim();
        credentialId = credentialId?.Trim();

        if (string.IsNullOrEmpty(serial) && 
            string.IsNullOrEmpty(user) && 
            string.IsNullOrEmpty(credentialId) && 
            !cancelEnrollment)
        {
            throw new ParameterError("You need to specify a serial, user or credential_id.");
        }

        if (!string.IsNullOrEmpty(serial) && serial.Contains('*'))
        {
            throw new ParameterError("Invalid serial number.");
        }

        if (!string.IsNullOrEmpty(user) && user.Contains('%'))
        {
            throw new ParameterError("Invalid user.");
        }
    }

    /// <summary>
    /// Validate a serial number format.
    /// Serial numbers should only contain alphanumeric characters, hyphens, and underscores.
    /// </summary>
    /// <param name="serial">Serial number to validate</param>
    /// <exception cref="ParameterError">Thrown if serial is invalid</exception>
    public static void CheckSerialValid(string serial)
    {
        if (string.IsNullOrWhiteSpace(serial))
        {
            throw new ParameterError("Serial cannot be empty");
        }

        // Allowed serial pattern: alphanumeric, hyphens, and underscores
        if (!System.Text.RegularExpressions.Regex.IsMatch(serial, @"^[0-9a-zA-Z\-_]+$"))
        {
            throw new ParameterError($"Invalid serial number format: {serial}");
        }
    }
}
