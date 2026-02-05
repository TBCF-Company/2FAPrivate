// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/server.py

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// RADIUS Server configuration
    /// Equivalent to Python's RADIUSServer class
    /// </summary>
    [Table("radiusserver")]
    [Index(nameof(Identifier), IsUnique = true)]
    public class RADIUSServer : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(255)]
        public string Identifier { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Server { get; set; }

        public int? Port { get; set; } = 1812;

        [StringLength(2000)]
        public string? Secret { get; set; }

        public int? Timeout { get; set; } = 5;

        public int? Retries { get; set; } = 3;

        [StringLength(2000)]
        public string? Description { get; set; }

        public RADIUSServer()
        {
        }

        public RADIUSServer(string identifier, string? server = null, int port = 1812, string? secret = null)
        {
            Identifier = identifier;
            Server = server;
            Port = port;
            Secret = secret;
        }
    }

    /// <summary>
    /// SMTP Server configuration for sending emails
    /// Equivalent to Python's SMTPServer class
    /// </summary>
    [Table("smtpserver")]
    [Index(nameof(Identifier), IsUnique = true)]
    public class SMTPServer : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(255)]
        public string Identifier { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Server { get; set; }

        public int? Port { get; set; } = 25;

        [StringLength(2000)]
        public string? Username { get; set; }

        [StringLength(2000)]
        public string? Password { get; set; }

        [StringLength(2000)]
        public string? Sender { get; set; }

        public bool Tls { get; set; } = false;

        [StringLength(2000)]
        public string? Description { get; set; }

        public int? Timeout { get; set; } = 10;

        [StringLength(2000)]
        public string? EnrollText { get; set; }

        public SMTPServer()
        {
        }

        public SMTPServer(string identifier, string? server = null, int port = 25)
        {
            Identifier = identifier;
            Server = server;
            Port = port;
        }
    }
}
