using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using Parlamento.Domain.Enums;

namespace Parlamento.Domain.Entities;

public class ProposalInteractionEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int UserId { get; set; }

    [JsonIgnore]
    public User? User { get; set; }

    public int ProjectLawId { get; set; }

    [JsonIgnore]
    public ProjectLaw? ProjectLaw { get; set; }

    [Required]
    public ProposalInteractionType InteractionType { get; set; }

    public string? IdempotencyKey { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
