// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace TwoFactorAuth.Core.Models;

/// <summary>
/// Device activation request model
/// </summary>
public class ActivationRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? Platform { get; set; }
    public string? OsVersion { get; set; }
    public string? Model { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
}
