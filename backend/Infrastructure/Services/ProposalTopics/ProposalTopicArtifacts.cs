using System.Text.Json;
using System.Text.Json.Serialization;

namespace Parlamento.Infrastructure.Services.ProposalTopics;

internal sealed class ProposalTopicTaxonomyArtifact
{
    public string Version { get; set; } = string.Empty;

    public DateTimeOffset? GeneratedAtUtc { get; set; }

    public List<ProposalTopicArtifactParent> Topics { get; set; } = [];
}

internal sealed class ProposalTopicArtifactParent
{
    public string Slug { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public List<ProposalTopicArtifactSubtopic> Subtopics { get; set; } = [];
}

internal sealed class ProposalTopicArtifactSubtopic
{
    public string Slug { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<JsonElement> SourceClusterIds { get; set; } = [];
}

internal sealed class ProposalTopicClassifierArtifact
{
    public string TaxonomyVersion { get; set; } = string.Empty;

    public ProposalTopicClassifierEmbedding Embedding { get; set; } = new();

    public ProposalTopicClassificationPolicy ClassificationPolicy { get; set; } = new();

    public List<ProposalTopicClassifierSubtopic> Subtopics { get; set; } = [];
}

internal sealed class ProposalTopicClassifierEmbedding
{
    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int Dimensions { get; set; }

    public string? Distance { get; set; }
}

internal sealed class ProposalTopicClassificationPolicy
{
    [JsonPropertyName("auto_similarity_threshold")]
    public double AutoSimilarityThreshold { get; set; } = 0.74;

    [JsonPropertyName("review_similarity_threshold")]
    public double ReviewSimilarityThreshold { get; set; } = 0.70;

    [JsonPropertyName("min_similarity_margin")]
    public double MinSimilarityMargin { get; set; } = 0.03;

    [JsonPropertyName("low_support_min_examples")]
    public int LowSupportMinExamples { get; set; } = 5;

    [JsonPropertyName("low_support_auto_threshold")]
    public double LowSupportAutoThreshold { get; set; } = 0.82;
}

internal sealed class ProposalTopicClassifierSubtopic
{
    public string TaxonomyVersion { get; set; } = string.Empty;

    public int CentroidIndex { get; set; }

    public string ParentTopicSlug { get; set; } = string.Empty;

    public string ParentTopicLabel { get; set; } = string.Empty;

    public string SubtopicSlug { get; set; } = string.Empty;

    public string SubtopicLabel { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SupportCount { get; set; }

    public string? SupportQuality { get; set; }

    public double SimilarityMin { get; set; }

    public double SimilarityP05 { get; set; }

    public double SimilarityP10 { get; set; }

    public double SimilarityP25 { get; set; }

    public double SimilarityMedian { get; set; }

    public double SimilarityMean { get; set; }

    public double RecommendedAutoThreshold { get; set; }

    public List<string> SourceProjectLawIds { get; set; } = [];

    public double[] Centroid { get; set; } = [];
}
