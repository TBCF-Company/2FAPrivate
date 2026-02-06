// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace TwoFactorAuth.Core.Models;

/// <summary>
/// Device activation validation model
/// </summary>
public class DeviceActivationValidation
{
    public string DeviceId { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Issuer { get; set; }
}
