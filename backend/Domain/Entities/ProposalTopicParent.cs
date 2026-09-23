using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Parlamento.Domain.Entities;

public class ProposalTopicParent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int TaxonomyVersionId { get; set; }

    [JsonIgnore]
    public ProposalTopicTaxonomyVersion? TaxonomyVersion { get; set; }

    [Required]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public string? SourceClusterIdsJson { get; set; }

    public List<ProposalSubtopic> Subtopics { get; set; } = [];
}
