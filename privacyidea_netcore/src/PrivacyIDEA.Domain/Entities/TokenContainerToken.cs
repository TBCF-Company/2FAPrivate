using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Associates tokens with containers
/// </summary>
[Table("tokencontainertoken")]
public class TokenContainerToken
{
    [Key]
    public int Id { get; set; }

    [Column("container_id")]
    public int ContainerId { get; set; }

    [Column("token_id")]
    public int TokenId { get; set; }

    // Navigation properties
    [ForeignKey("ContainerId")]
    public TokenContainer? Container { get; set; }

    [ForeignKey("TokenId")]
    public Token? Token { get; set; }
}
