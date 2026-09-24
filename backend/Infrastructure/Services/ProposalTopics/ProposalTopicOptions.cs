namespace Parlamento.Infrastructure.Services.ProposalTopics;

public class ProposalTopicOptions
{
    public const string SectionName = "ProposalTopics";

    public string TaxonomyVersion { get; set; } = "taxonomy_v2";

    public string ArtifactDirectory { get; set; } = Path.Combine("Data", "ProposalTopics", "taxonomy_v2");

    public string TaxonomyFileName { get; set; } = "taxonomy_v2.json";

    public string ClassifierFileName { get; set; } = Path.Combine("classifier", "taxonomy_v2_centroid_classifier.json");

    public string SeedAssignmentsFileName { get; set; } =
        Path.Combine("classifier", "taxonomy_v2_project_law_topic_seed_assignments.csv");

    public string? ApiKey { get; set; }

    public string EmbeddingModel { get; set; } = "text-embedding-3-large";

    public int ChunkWordCount { get; set; } = 750;

    public int ChunkOverlapWordCount { get; set; } = 75;

    public int MinimumRedactedWordCount { get; set; } = 20;

    public string ResolveArtifactPath(string fileName)
    {
        var artifactDirectory = Path.IsPathRooted(ArtifactDirectory)
            ? ArtifactDirectory
            : Path.Combine(AppContext.BaseDirectory, ArtifactDirectory);

        return Path.GetFullPath(Path.Combine(artifactDirectory, fileName));
    }
}
