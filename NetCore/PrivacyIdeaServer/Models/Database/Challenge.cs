// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/challenge.py

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// Challenge table for challenge-response authentication
    /// Equivalent to Python's Challenge class
    /// </summary>
    [Table("challenge")]
    [Index(nameof(Serial))]
    [Index(nameof(TransactionId), IsUnique = true)]
    [Index(nameof(Timestamp))]
    public class Challenge : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [StringLength(40)]
        public string? Serial { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string TransactionId { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Challenge1 { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Data { get; set; } = string.Empty;

        [StringLength(64)]
        public string? Session { get; set; } = string.Empty;

        public DateTime? Timestamp { get; set; }

        public DateTime? ReceivedTimestamp { get; set; }

        /// <summary>
        /// Number of times the challenge was received/checked
        /// </summary>
        public int ReceivedCount { get; set; } = 0;

        public int OtpLen { get; set; } = 6;

        public bool OtpValid { get; set; } = false;

        public Challenge()
        {
            Timestamp = DateTime.UtcNow;
        }

        public Challenge(string serial, string transactionId, string? challenge = null)
        {
            Serial = serial;
            TransactionId = transactionId;
            Challenge1 = challenge;
            Timestamp = DateTime.UtcNow;
            ReceivedCount = 0;
            OtpValid = false;
        }
    }
}
