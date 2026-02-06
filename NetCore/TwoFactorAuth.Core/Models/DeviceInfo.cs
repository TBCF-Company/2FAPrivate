// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace TwoFactorAuth.Core.Models;

/// <summary>
/// Device information model
/// </summary>
public class DeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public bool IsActivated { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? ActivationToken { get; set; }
}
