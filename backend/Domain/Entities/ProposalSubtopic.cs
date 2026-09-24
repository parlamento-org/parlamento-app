using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Parlamento.Domain.Entities;

public class ProposalSubtopic
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int TaxonomyVersionId { get; set; }

    [JsonIgnore]
    public ProposalTopicTaxonomyVersion? TaxonomyVersion { get; set; }

    public int ParentTopicId { get; set; }

    [JsonIgnore]
    public ProposalTopicParent? ParentTopic { get; set; }

    [Required]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public string? SourceClusterIdsJson { get; set; }

    public int? CentroidIndex { get; set; }

    public int? SupportCount { get; set; }

    public string? SupportQuality { get; set; }

    public double? SimilarityMin { get; set; }

    public double? SimilarityP05 { get; set; }

    public double? SimilarityP10 { get; set; }

    public double? SimilarityP25 { get; set; }

    public double? SimilarityMedian { get; set; }

    public double? SimilarityMean { get; set; }

    public double? RecommendedAutoThreshold { get; set; }

    public List<ProjectLawTopicAssignment> ProjectLawAssignments { get; set; } = [];

    public List<ProjectLawTopicAssignment> BestProjectLawAssignments { get; set; } = [];

    public List<ProjectLawTopicAssignment> SecondBestProjectLawAssignments { get; set; } = [];
}
