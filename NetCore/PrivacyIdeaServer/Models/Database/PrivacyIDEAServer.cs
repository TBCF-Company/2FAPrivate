// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/server.py (PrivacyIDEAServer)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// Remote PrivacyIDEA Server configuration for federation
    /// Equivalent to Python's PrivacyIDEAServer class
    /// </summary>
    [Table("privacyideaserver")]
    [Index(nameof(Identifier), IsUnique = true)]
    public class PrivacyIDEAServer : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(255)]
        public string Identifier { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Url { get; set; }

        public bool Tls { get; set; } = true;

        [StringLength(2000)]
        public string? Description { get; set; }

        public PrivacyIDEAServer()
        {
        }

        public PrivacyIDEAServer(string identifier, string? url = null, bool tls = true, string? description = null)
        {
            Identifier = identifier;
            Url = url;
            Tls = tls;
            Description = description;
        }
    }
}
