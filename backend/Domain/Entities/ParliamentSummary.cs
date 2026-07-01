using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Parlamento.Domain.Entities;

public class ParliamentSummary
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectLawId { get; set; }

    [JsonIgnore]
    public ProjectLaw? ProjectLaw { get; set; }

    public int ParliamentDocumentContentId { get; set; }

    [JsonIgnore]
    public ParliamentDocumentContent? ParliamentDocumentContent { get; set; }

    public string? ShortTitle { get; set; }

    public string? SummaryText { get; set; }

    public string? BulletPointsJson { get; set; }

    [Required]
    public string GenerationStatus { get; set; } = "NotStarted";

    [Required]
    public string ModelName { get; set; } = string.Empty;

    [Required]
    public string PromptVersion { get; set; } = string.Empty;

    [Required]
    public string SourceDocumentHash { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? ErrorDetails { get; set; }

    public DateTime? GeneratedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
