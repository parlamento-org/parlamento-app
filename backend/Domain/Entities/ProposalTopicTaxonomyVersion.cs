using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class ProposalTopicTaxonomyVersion
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string Version { get; set; } = string.Empty;

    public DateTime? GeneratedAtUtc { get; set; }

    [Required]
    public string EmbeddingProvider { get; set; } = string.Empty;

    [Required]
    public string EmbeddingModel { get; set; } = string.Empty;

    public int? EmbeddingDimensions { get; set; }

    public double? AutoSimilarityThreshold { get; set; }

    public double? ReviewSimilarityThreshold { get; set; }

    public double? MinSimilarityMargin { get; set; }

    public int? LowSupportMinExamples { get; set; }

    public double? LowSupportAutoThreshold { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ProposalTopicParent> ParentTopics { get; set; } = [];

    public List<ProposalSubtopic> Subtopics { get; set; } = [];

    public List<ProjectLawTopicAssignment> ProjectLawAssignments { get; set; } = [];
}
