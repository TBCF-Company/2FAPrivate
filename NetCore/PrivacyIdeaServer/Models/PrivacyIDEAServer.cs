// SPDX-License-Identifier: AGPL-3.0-or-later
// Port of privacyIDEA server model to .NET Core 8

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIdeaServer.Models
{
    /// <summary>
    /// Database model for PrivacyIDEA Server configuration
    /// Equivalent to Python's PrivacyIDEAServerDB model
    /// </summary>
    [Table("privacyideaserver")]
    public class PrivacyIDEAServerDB
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Url { get; set; } = string.Empty;

        public bool Tls { get; set; } = true;

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
    }
}
