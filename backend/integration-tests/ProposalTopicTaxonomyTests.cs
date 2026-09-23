using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Topics;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;
using Parlamento.Infrastructure.Services.ProposalTopics;

using Xunit;

namespace integration_tests;

public class ProposalTopicTaxonomyTests
{
    [Fact]
    public async Task ImportReviewedTaxonomyAsync_ImportsParentsAndSubtopicsIdempotently()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var artifactDirectory = await CreateArtifactsAsync();
        var service = CreateImportService(context, artifactDirectory);

        var first = await service.ImportReviewedTaxonomyAsync();
        var second = await service.ImportReviewedTaxonomyAsync();

        Assert.Equal(1, first.ParentTopicsInserted);
        Assert.Equal(2, first.SubtopicsInserted);
        Assert.Equal(0, second.ParentTopicsInserted);
        Assert.Equal(0, second.SubtopicsInserted);
        Assert.Equal(1, await context.ProposalTopicTaxonomyVersions.CountAsync());
        Assert.Equal(1, await context.ProposalTopicParents.CountAsync());
        Assert.Equal(2, await context.ProposalSubtopics.CountAsync());

        var subtopic = await context.ProposalSubtopics.SingleAsync(x => x.Slug == "topic_a");
        Assert.Equal(0, subtopic.CentroidIndex);
        Assert.Equal("strong", subtopic.SupportQuality);
    }

    [Fact]
    public async Task ImportReviewedTaxonomyAsync_ImportsHistoricalAssignmentsIdempotently()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var fixture = await AddProjectLawWithContentAsync(context, projectLawId: 777, redactedText: "texto redigido suficiente");
        var seedCsv = BuildSeedCsv(fixture.ProjectLaw.Id, fixture.Content.Id);
        var artifactDirectory = await CreateArtifactsAsync(seedCsv);
        var service = CreateImportService(context, artifactDirectory);

        var first = await service.ImportReviewedTaxonomyAsync();
        var second = await service.ImportReviewedTaxonomyAsync();

        Assert.Equal(1, first.SeedAssignmentsInserted);
        Assert.Equal(1, second.SeedAssignmentsSkipped);
        Assert.Equal(1, await context.ProjectLawTopicAssignments.CountAsync());

        var assignment = await context.ProjectLawTopicAssignments
            .Include(x => x.Subtopic)
            .SingleAsync();
        Assert.Equal("hdbscan_cluster_human_reviewed_taxonomy", assignment.AssignmentMethod);
        Assert.Equal("accepted_cluster", assignment.AssignmentStatus);
        Assert.Equal("topic_a", assignment.Subtopic!.Slug);
        Assert.Equal(assignment.SubtopicId, assignment.BestSubtopicId);
    }

    [Fact]
    public async Task AssignMissingAsync_AssignsKnownVectorToCentroidWithHighConfidence()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var artifactDirectory = await ImportTaxonomyAsync(context);
        var fixture = await AddProjectLawWithContentAsync(context, redactedText: "energia renovavel comunidades locais");
        var service = CreateAssignmentService(context, artifactDirectory, new FakeTopicEmbeddingClient([1, 0, 0]));

        var result = await service.AssignMissingAsync(new ProjectLawTopicAssignmentRequest
        {
            ProjectLawId = fixture.ProjectLaw.Id
        });

        Assert.Equal(1, result.AssignmentsCreated);
        Assert.Equal(1, result.AutoAssigned);

        var assignment = await context.ProjectLawTopicAssignments
            .Include(x => x.Subtopic)
            .SingleAsync();
        Assert.Equal("assigned", assignment.AssignmentStatus);
        Assert.Equal("centroid_auto_assigned", assignment.AssignmentMethod);
        Assert.Equal("topic_a", assignment.Subtopic!.Slug);
        Assert.True(assignment.BestSimilarity > 0.99);
        Assert.True(assignment.SimilarityMargin > 0.99);
    }

    [Fact]
    public async Task AssignMissingAsync_LeavesLowSimilarityProposalUnassigned()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var artifactDirectory = await ImportTaxonomyAsync(context);
        var fixture = await AddProjectLawWithContentAsync(context, redactedText: "texto redigido sem proximidade tematica");
        var service = CreateAssignmentService(context, artifactDirectory, new FakeTopicEmbeddingClient([0, 0, 1]));

        var result = await service.AssignMissingAsync(new ProjectLawTopicAssignmentRequest
        {
            ProjectLawId = fixture.ProjectLaw.Id
        });

        Assert.Equal(1, result.Unassigned);
        var assignment = await context.ProjectLawTopicAssignments.SingleAsync();
        Assert.Equal("unassigned", assignment.AssignmentStatus);
        Assert.Null(assignment.SubtopicId);
        Assert.NotNull(assignment.BestSubtopicId);
        Assert.Equal(0, assignment.BestSimilarity);
    }

    [Fact]
    public async Task AssignMissingAsync_MarksAmbiguousNearThresholdMatchAsNeedsReview()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var artifactDirectory = await ImportTaxonomyAsync(context);
        var fixture = await AddProjectLawWithContentAsync(context, redactedText: "texto redigido ambiguo");
        var service = CreateAssignmentService(
            context,
            artifactDirectory,
            new FakeTopicEmbeddingClient([0.70710678, 0.70710678, 0]));

        var result = await service.AssignMissingAsync(new ProjectLawTopicAssignmentRequest
        {
            ProjectLawId = fixture.ProjectLaw.Id
        });

        Assert.Equal(1, result.NeedsReview);
        var assignment = await context.ProjectLawTopicAssignments.SingleAsync();
        Assert.Equal("needs_review", assignment.AssignmentStatus);
        Assert.Equal("centroid_needs_review", assignment.AssignmentMethod);
        Assert.NotNull(assignment.SubtopicId);
        Assert.True(assignment.SimilarityMargin < 0.01);
    }

    [Fact]
    public async Task AssignMissingAsync_DoesNotEmbedWhenRedactedTextIsMissing()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var artifactDirectory = await ImportTaxonomyAsync(context);
        var fixture = await AddProjectLawWithContentAsync(context, redactedText: null);
        var fakeClient = new FakeTopicEmbeddingClient([1, 0, 0]);
        var service = CreateAssignmentService(context, artifactDirectory, fakeClient);

        var result = await service.AssignMissingAsync(new ProjectLawTopicAssignmentRequest
        {
            ProjectLawId = fixture.ProjectLaw.Id
        });

        Assert.Equal(1, result.MissingRedactedText);
        Assert.Equal(0, fakeClient.Calls);
        var assignment = await context.ProjectLawTopicAssignments.SingleAsync();
        Assert.Equal("unassigned", assignment.AssignmentStatus);
        Assert.Null(assignment.SubtopicId);
        Assert.Null(assignment.BestSubtopicId);
    }

    [Fact]
    public async Task AssignMissingAsync_PreservesReviewedAssignment()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var artifactDirectory = await ImportTaxonomyAsync(context);
        var fixture = await AddProjectLawWithContentAsync(context, redactedText: "energia renovavel comunidades locais");
        var taxonomy = await context.ProposalTopicTaxonomyVersions.SingleAsync();
        var reviewedSubtopic = await context.ProposalSubtopics.SingleAsync(x => x.Slug == "topic_b");
        context.ProjectLawTopicAssignments.Add(new ProjectLawTopicAssignment
        {
            ProjectLawId = fixture.ProjectLaw.Id,
            ParliamentDocumentContentId = fixture.Content.Id,
            TaxonomyVersionId = taxonomy.Id,
            SubtopicId = reviewedSubtopic.Id,
            BestSubtopicId = reviewedSubtopic.Id,
            AssignmentMethod = "seeded_human_reviewed",
            AssignmentStatus = "assigned",
            AssignmentConfidence = 1,
            EmbeddingProvider = "openai",
            EmbeddingModel = "text-embedding-3-large",
            SourceDocumentHash = fixture.Content.RedactedContentHash,
            IsCurrent = true,
            AssignedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateAssignmentService(context, artifactDirectory, new FakeTopicEmbeddingClient([1, 0, 0]));
        var result = await service.AssignMissingAsync(new ProjectLawTopicAssignmentRequest
        {
            ProjectLawId = fixture.ProjectLaw.Id,
            Force = true
        });

        Assert.Equal(1, result.PreservedReviewedAssignments);
        Assert.Equal(1, await context.ProjectLawTopicAssignments.CountAsync());
        var assignment = await context.ProjectLawTopicAssignments.SingleAsync();
        Assert.Equal(reviewedSubtopic.Id, assignment.SubtopicId);
        Assert.Equal("seeded_human_reviewed", assignment.AssignmentMethod);
    }

    private static async Task<string> ImportTaxonomyAsync(DatabaseContext context)
    {
        var artifactDirectory = await CreateArtifactsAsync();
        var importService = CreateImportService(context, artifactDirectory);
        await importService.ImportReviewedTaxonomyAsync();
        return artifactDirectory;
    }

    private static ProposalTopicTaxonomyImportService CreateImportService(
        DatabaseContext context,
        string artifactDirectory)
    {
        return new ProposalTopicTaxonomyImportService(
            context,
            Options.Create(CreateOptions(artifactDirectory)),
            NullLogger<ProposalTopicTaxonomyImportService>.Instance);
    }

    private static ProjectLawTopicAssignmentService CreateAssignmentService(
        DatabaseContext context,
        string artifactDirectory,
        ITopicEmbeddingClient embeddingClient)
    {
        return new ProjectLawTopicAssignmentService(
            context,
            embeddingClient,
            Options.Create(CreateOptions(artifactDirectory)),
            NullLogger<ProjectLawTopicAssignmentService>.Instance);
    }

    private static ProposalTopicOptions CreateOptions(string artifactDirectory)
    {
        return new ProposalTopicOptions
        {
            TaxonomyVersion = "taxonomy_test",
            ArtifactDirectory = artifactDirectory,
            MinimumRedactedWordCount = 1,
            ChunkWordCount = 750,
            ChunkOverlapWordCount = 75,
            EmbeddingModel = "text-embedding-3-large"
        };
    }

    private static async Task<(ProjectLaw ProjectLaw, ParliamentInitiativeDocument Document, ParliamentDocumentContent Content)>
        AddProjectLawWithContentAsync(
            DatabaseContext context,
            int? projectLawId = null,
            string? redactedText = "texto redigido suficiente")
    {
        var party = await context.PoliticalParties.SingleAsync(x => x.partyAcronym == "CH");
        var projectLaw = new ProjectLaw
        {
            Id = projectLawId.GetValueOrDefault(),
            SourceId = projectLawId.GetValueOrDefault(Random.Shared.Next(10000, 90000)),
            SourceIdText = projectLawId?.ToString() ?? Guid.NewGuid().ToString("N"),
            Legislatura = "XVII",
            VoteDate = "2026-01-01",
            ProposingParty = party,
            ProposalTitle = "Teste topico",
            FullProposalTextLink = "topic-test.txt"
        };
        if (!projectLawId.HasValue)
        {
            projectLaw.Id = 0;
        }

        var document = new ParliamentInitiativeDocument
        {
            Scope = "InitiativeText",
            Name = "Texto da iniciativa",
            Url = "topic-test.txt"
        };
        projectLaw.ImportedDocuments.Add(document);
        context.ProjectLaws.Add(projectLaw);
        await context.SaveChangesAsync();

        var content = new ParliamentDocumentContent
        {
            ProjectLawId = projectLaw.Id,
            ParliamentInitiativeDocumentId = document.Id,
            SourceUrl = "topic-test.txt",
            SourceContentHash = $"source-{projectLaw.Id}",
            RedactedContentHash = $"redacted-{projectLaw.Id}",
            RedactedContentText = redactedText,
            ExtractionStatus = "Succeeded",
            RedactionStatus = "Succeeded"
        };
        context.ParliamentDocumentContents.Add(content);
        await context.SaveChangesAsync();

        return (projectLaw, document, content);
    }

    private static async Task<string> CreateArtifactsAsync(string? seedCsv = null)
    {
        var artifactDirectory = Path.Combine(Path.GetTempPath(), $"topic-taxonomy-tests-{Guid.NewGuid():N}");
        var classifierDirectory = Path.Combine(artifactDirectory, "classifier");
        Directory.CreateDirectory(classifierDirectory);

        var taxonomy = new
        {
            version = "taxonomy_test",
            generatedAtUtc = "2026-09-23T00:00:00Z",
            topics = new object[]
            {
                new
                {
                    slug = "parent_topic",
                    label = "Parent Topic",
                    description = "",
                    displayOrder = 1,
                    subtopics = new object[]
                    {
                        new
                        {
                            slug = "topic_a",
                            label = "Topic A",
                            description = "Topic A description",
                            sourceClusterIds = new[] { 1 }
                        },
                        new
                        {
                            slug = "topic_b",
                            label = "Topic B",
                            description = "Topic B description",
                            sourceClusterIds = new[] { 2 }
                        }
                    }
                }
            }
        };

        var classifier = new Dictionary<string, object?>
        {
            ["artifactType"] = "parlamento-topic-centroid-classifier",
            ["taxonomyVersion"] = "taxonomy_test",
            ["embedding"] = new
            {
                provider = "openai",
                model = "text-embedding-3-large",
                dimensions = 3,
                distance = "cosine_similarity_on_l2_normalized_vectors"
            },
            ["classificationPolicy"] = new Dictionary<string, object>
            {
                ["auto_similarity_threshold"] = 0.74,
                ["review_similarity_threshold"] = 0.70,
                ["min_similarity_margin"] = 0.03,
                ["low_support_min_examples"] = 5,
                ["low_support_auto_threshold"] = 0.82
            },
            ["subtopics"] = new object[]
            {
                BuildClassifierSubtopic(0, "topic_a", "Topic A", [1, 0, 0]),
                BuildClassifierSubtopic(1, "topic_b", "Topic B", [0, 1, 0])
            }
        };

        await File.WriteAllTextAsync(
            Path.Combine(artifactDirectory, "taxonomy_v2.json"),
            JsonSerializer.Serialize(taxonomy, JsonOptions));
        await File.WriteAllTextAsync(
            Path.Combine(classifierDirectory, "taxonomy_v2_centroid_classifier.json"),
            JsonSerializer.Serialize(classifier, JsonOptions));
        await File.WriteAllTextAsync(
            Path.Combine(classifierDirectory, "taxonomy_v2_project_law_topic_seed_assignments.csv"),
            seedCsv ?? BuildSeedCsv());

        return artifactDirectory;
    }

    private static Dictionary<string, object?> BuildClassifierSubtopic(
        int centroidIndex,
        string slug,
        string label,
        double[] centroid)
    {
        return new Dictionary<string, object?>
        {
            ["taxonomyVersion"] = "taxonomy_test",
            ["centroidIndex"] = centroidIndex,
            ["parentTopicSlug"] = "parent_topic",
            ["parentTopicLabel"] = "Parent Topic",
            ["subtopicSlug"] = slug,
            ["subtopicLabel"] = label,
            ["description"] = $"{label} description",
            ["supportCount"] = 10,
            ["supportQuality"] = "strong",
            ["similarityMin"] = 0.8,
            ["similarityP05"] = 0.81,
            ["similarityP10"] = 0.82,
            ["similarityP25"] = 0.83,
            ["similarityMedian"] = 0.84,
            ["similarityMean"] = 0.85,
            ["recommendedAutoThreshold"] = 0.74,
            ["sourceProjectLawIds"] = new[] { "1" },
            ["centroid"] = centroid
        };
    }

    private static string BuildSeedCsv(int? projectLawId = null, int? documentContentId = null)
    {
        const string header = "taxonomy_version,ProjectLawId,ParliamentDocumentContentId,Legislature,parent_topic_slug,parent_topic_label,subtopic_slug,subtopic_label,assignment_method,assignment_status,assignment_confidence,cluster_id,cluster_probability,is_seed_assignment";
        if (!projectLawId.HasValue)
        {
            return header + Environment.NewLine;
        }

        return header + Environment.NewLine +
               $"taxonomy_test,{projectLawId},{documentContentId},XVII,parent_topic,Parent Topic,topic_a,Topic A,hdbscan_cluster_human_reviewed_taxonomy,accepted_cluster,0.91,1,0.91,True{Environment.NewLine}";
    }

    private static DatabaseContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase($"proposal-topic-tests-{Guid.NewGuid()}")
            .Options;

        return new DatabaseContext(options);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed class FakeTopicEmbeddingClient : ITopicEmbeddingClient
    {
        private readonly IReadOnlyList<double> _embedding;

        public FakeTopicEmbeddingClient(IReadOnlyList<double> embedding)
        {
            _embedding = embedding;
        }

        public string Provider => "openai";

        public string ModelName => "text-embedding-3-large";

        public int Calls { get; private set; }

        public Task<IReadOnlyList<IReadOnlyList<double>>> GenerateEmbeddingsAsync(
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<IReadOnlyList<double>>>(
                inputs.Select(_ => _embedding).ToList());
        }
    }
}
