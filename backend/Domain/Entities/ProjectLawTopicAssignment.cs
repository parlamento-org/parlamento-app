using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Parlamento.Domain.Entities;

public class ProjectLawTopicAssignment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectLawId { get; set; }

    [JsonIgnore]
    public ProjectLaw? ProjectLaw { get; set; }

    public int? ParliamentDocumentContentId { get; set; }

    [JsonIgnore]
    public ParliamentDocumentContent? ParliamentDocumentContent { get; set; }

    public int TaxonomyVersionId { get; set; }

    [JsonIgnore]
    public ProposalTopicTaxonomyVersion? TaxonomyVersion { get; set; }

    public int? SubtopicId { get; set; }

    [JsonIgnore]
    public ProposalSubtopic? Subtopic { get; set; }

    public int? BestSubtopicId { get; set; }

    [JsonIgnore]
    public ProposalSubtopic? BestSubtopic { get; set; }

    public int? SecondBestSubtopicId { get; set; }

    [JsonIgnore]
    public ProposalSubtopic? SecondBestSubtopic { get; set; }

    [Required]
    public string AssignmentMethod { get; set; } = string.Empty;

    [Required]
    public string AssignmentStatus { get; set; } = string.Empty;

    public double? AssignmentConfidence { get; set; }

    public double? BestSimilarity { get; set; }

    public double? SecondBestSimilarity { get; set; }

    public double? SimilarityMargin { get; set; }

    [Required]
    public string EmbeddingProvider { get; set; } = string.Empty;

    [Required]
    public string EmbeddingModel { get; set; } = string.Empty;

    public string? SourceDocumentHash { get; set; }

    public bool IsCurrent { get; set; } = true;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
