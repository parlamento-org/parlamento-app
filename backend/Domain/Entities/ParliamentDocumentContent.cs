using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Parlamento.Domain.Entities;

public class ParliamentDocumentContent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectLawId { get; set; }

    [JsonIgnore]
    public ProjectLaw? ProjectLaw { get; set; }

    public int ParliamentInitiativeDocumentId { get; set; }

    [JsonIgnore]
    public ParliamentInitiativeDocument? ParliamentInitiativeDocument { get; set; }

    public string? SourceUrl { get; set; }

    public string? SourceContentHash { get; set; }

    public long? SourceContentLength { get; set; }

    public string? ExtractedContentHash { get; set; }

    public string? RedactedContentHash { get; set; }

    public string? RedactedContentText { get; set; }

    public string? RedactedContentHtml { get; set; }

    [Required]
    public string ExtractionStatus { get; set; } = "NotStarted";

    [Required]
    public string RedactionStatus { get; set; } = "NotStarted";

    public string? ExtractorKind { get; set; }

    public string? RedactionPolicyVersion { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? ExtractedAtUtc { get; set; }

    public DateTime? RedactedAtUtc { get; set; }
}
