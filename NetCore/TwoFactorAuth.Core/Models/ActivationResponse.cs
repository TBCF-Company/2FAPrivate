// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace TwoFactorAuth.Core.Models;

/// <summary>
/// Device activation response model
/// </summary>
public class ActivationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public string? OtpCode { get; set; }
    public string? DeviceId { get; set; }
}
