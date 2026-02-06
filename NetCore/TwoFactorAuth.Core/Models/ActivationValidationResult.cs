// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace TwoFactorAuth.Core.Models;

/// <summary>
/// Activation validation result model
/// </summary>
public class ActivationValidationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ActivationToken { get; set; }
    public DateTime? ActivatedAt { get; set; }
}
