using System.Globalization;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Topics;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services.ProposalTopics;

public class ProposalTopicTaxonomyImportService : IProposalTopicTaxonomyImportService
{
    private readonly DatabaseContext _context;
    private readonly ProposalTopicOptions _options;
    private readonly ILogger<ProposalTopicTaxonomyImportService> _logger;

    public ProposalTopicTaxonomyImportService(
        DatabaseContext context,
        IOptions<ProposalTopicOptions> options,
        ILogger<ProposalTopicTaxonomyImportService> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ProposalTopicTaxonomyImportResult> ImportReviewedTaxonomyAsync(
        CancellationToken cancellationToken = default)
    {
        var taxonomy = await LoadJsonAsync<ProposalTopicTaxonomyArtifact>(
            _options.ResolveArtifactPath(_options.TaxonomyFileName),
            cancellationToken);
        var classifier = await LoadJsonAsync<ProposalTopicClassifierArtifact>(
            _options.ResolveArtifactPath(_options.ClassifierFileName),
            cancellationToken);

        if (!string.Equals(taxonomy.Version, classifier.TaxonomyVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Taxonomy artifact version '{taxonomy.Version}' does not match classifier version '{classifier.TaxonomyVersion}'.");
        }

        var now = DateTime.UtcNow;
        var taxonomyVersion = await _context.ProposalTopicTaxonomyVersions
            .SingleOrDefaultAsync(x => x.Version == taxonomy.Version, cancellationToken);

        if (taxonomyVersion is null)
        {
            taxonomyVersion = new ProposalTopicTaxonomyVersion
            {
                Version = taxonomy.Version,
                CreatedAtUtc = now
            };
            _context.ProposalTopicTaxonomyVersions.Add(taxonomyVersion);
        }

        UpdateTaxonomyVersion(taxonomyVersion, taxonomy, classifier, now);
        await _context.SaveChangesAsync(cancellationToken);

        var parentCounts = await UpsertParentsAsync(taxonomyVersion, taxonomy, cancellationToken);
        var subtopicCounts = await UpsertSubtopicsAsync(taxonomyVersion, taxonomy, classifier, cancellationToken);
        var seedCounts = await ImportSeedAssignmentsAsync(taxonomyVersion, cancellationToken);

        _logger.LogInformation(
            "Imported proposal topic taxonomy {TaxonomyVersion}. Parents read={ParentsRead} inserted={ParentsInserted} updated={ParentsUpdated}; subtopics read={SubtopicsRead} inserted={SubtopicsInserted} updated={SubtopicsUpdated}; seed assignments read={SeedAssignmentsRead} inserted={SeedAssignmentsInserted} updated={SeedAssignmentsUpdated} skipped={SeedAssignmentsSkipped} missingProjectLaws={SeedAssignmentsMissingProjectLaws} missingSubtopics={SeedAssignmentsMissingSubtopics}.",
            taxonomyVersion.Version,
            parentCounts.Read,
            parentCounts.Inserted,
            parentCounts.Updated,
            subtopicCounts.Read,
            subtopicCounts.Inserted,
            subtopicCounts.Updated,
            seedCounts.Read,
            seedCounts.Inserted,
            seedCounts.Updated,
            seedCounts.Skipped,
            seedCounts.MissingProjectLaws,
            seedCounts.MissingSubtopics);

        return new ProposalTopicTaxonomyImportResult(
            parentCounts.Read,
            parentCounts.Inserted,
            parentCounts.Updated,
            subtopicCounts.Read,
            subtopicCounts.Inserted,
            subtopicCounts.Updated,
            seedCounts.Read,
            seedCounts.Inserted,
            seedCounts.Updated,
            seedCounts.Skipped,
            seedCounts.MissingProjectLaws,
            seedCounts.MissingSubtopics);
    }

    private static void UpdateTaxonomyVersion(
        ProposalTopicTaxonomyVersion taxonomyVersion,
        ProposalTopicTaxonomyArtifact taxonomy,
        ProposalTopicClassifierArtifact classifier,
        DateTime now)
    {
        taxonomyVersion.GeneratedAtUtc = taxonomy.GeneratedAtUtc?.UtcDateTime;
        taxonomyVersion.EmbeddingProvider = classifier.Embedding.Provider;
        taxonomyVersion.EmbeddingModel = classifier.Embedding.Model;
        taxonomyVersion.EmbeddingDimensions = classifier.Embedding.Dimensions;
        taxonomyVersion.AutoSimilarityThreshold = classifier.ClassificationPolicy.AutoSimilarityThreshold;
        taxonomyVersion.ReviewSimilarityThreshold = classifier.ClassificationPolicy.ReviewSimilarityThreshold;
        taxonomyVersion.MinSimilarityMargin = classifier.ClassificationPolicy.MinSimilarityMargin;
        taxonomyVersion.LowSupportMinExamples = classifier.ClassificationPolicy.LowSupportMinExamples;
        taxonomyVersion.LowSupportAutoThreshold = classifier.ClassificationPolicy.LowSupportAutoThreshold;
        taxonomyVersion.UpdatedAtUtc = now;
    }

    private async Task<UpsertCounts> UpsertParentsAsync(
        ProposalTopicTaxonomyVersion taxonomyVersion,
        ProposalTopicTaxonomyArtifact taxonomy,
        CancellationToken cancellationToken)
    {
        var existing = await _context.ProposalTopicParents
            .Where(x => x.TaxonomyVersionId == taxonomyVersion.Id)
            .ToDictionaryAsync(x => x.Slug, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var counts = new UpsertCounts(taxonomy.Topics.Count);

        foreach (var topic in taxonomy.Topics.OrderBy(x => x.DisplayOrder))
        {
            if (!existing.TryGetValue(topic.Slug, out var parent))
            {
                parent = new ProposalTopicParent
                {
                    TaxonomyVersionId = taxonomyVersion.Id,
                    Slug = topic.Slug
                };
                _context.ProposalTopicParents.Add(parent);
                counts.Inserted++;
            }

            if (UpdateParent(parent, topic))
            {
                counts.Updated += parent.Id == 0 ? 0 : 1;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return counts;
    }

    private async Task<UpsertCounts> UpsertSubtopicsAsync(
        ProposalTopicTaxonomyVersion taxonomyVersion,
        ProposalTopicTaxonomyArtifact taxonomy,
        ProposalTopicClassifierArtifact classifier,
        CancellationToken cancellationToken)
    {
        var parents = await _context.ProposalTopicParents
            .Where(x => x.TaxonomyVersionId == taxonomyVersion.Id)
            .ToDictionaryAsync(x => x.Slug, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var classifierBySlug = classifier.Subtopics
            .ToDictionary(x => x.SubtopicSlug, StringComparer.OrdinalIgnoreCase);

        var existing = await _context.ProposalSubtopics
            .Where(x => x.TaxonomyVersionId == taxonomyVersion.Id)
            .ToDictionaryAsync(x => x.Slug, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var subtopicArtifacts = taxonomy.Topics
            .SelectMany(topic => topic.Subtopics.Select((subtopic, index) => new
            {
                Parent = topic,
                Subtopic = subtopic,
                DisplayOrder = index + 1
            }))
            .ToList();
        var counts = new UpsertCounts(subtopicArtifacts.Count);

        foreach (var artifact in subtopicArtifacts)
        {
            var parent = parents[artifact.Parent.Slug];
            if (!existing.TryGetValue(artifact.Subtopic.Slug, out var subtopic))
            {
                subtopic = new ProposalSubtopic
                {
                    TaxonomyVersionId = taxonomyVersion.Id,
                    Slug = artifact.Subtopic.Slug
                };
                _context.ProposalSubtopics.Add(subtopic);
                counts.Inserted++;
            }

            classifierBySlug.TryGetValue(artifact.Subtopic.Slug, out var classifierSubtopic);
            if (UpdateSubtopic(subtopic, parent.Id, artifact.Subtopic, artifact.DisplayOrder, classifierSubtopic))
            {
                counts.Updated += subtopic.Id == 0 ? 0 : 1;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return counts;
    }

    private async Task<SeedAssignmentCounts> ImportSeedAssignmentsAsync(
        ProposalTopicTaxonomyVersion taxonomyVersion,
        CancellationToken cancellationToken)
    {
        var path = _options.ResolveArtifactPath(_options.SeedAssignmentsFileName);
        var rows = ReadCsv(path);
        var counts = new SeedAssignmentCounts(rows.Count);
        if (rows.Count == 0)
        {
            return counts;
        }

        var projectLawIds = rows
            .Select(x => ParseInt(GetValue(x, "ProjectLawId")))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var projectLawIdSet = (await _context.ProjectLaws
            .Where(x => projectLawIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var documentContentIds = rows
            .Select(x => ParseInt(GetValue(x, "ParliamentDocumentContentId")))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var documentContentIdSet = (await _context.ParliamentDocumentContents
            .Where(x => documentContentIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var subtopics = await _context.ProposalSubtopics
            .Where(x => x.TaxonomyVersionId == taxonomyVersion.Id)
            .ToDictionaryAsync(x => x.Slug, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var currentAssignments = (await _context.ProjectLawTopicAssignments
                .Where(x => x.TaxonomyVersionId == taxonomyVersion.Id)
                .Where(x => x.IsCurrent)
                .Where(x => projectLawIds.Contains(x.ProjectLawId))
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.ProjectLawId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.UpdatedAtUtc).First());

        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            var projectLawId = ParseInt(GetValue(row, "ProjectLawId"));
            if (!projectLawId.HasValue || !projectLawIdSet.Contains(projectLawId.Value))
            {
                counts.MissingProjectLaws++;
                continue;
            }

            var subtopicSlug = GetValue(row, "subtopic_slug");
            if (string.IsNullOrWhiteSpace(subtopicSlug) ||
                !subtopics.TryGetValue(subtopicSlug, out var subtopic))
            {
                counts.MissingSubtopics++;
                continue;
            }

            var method = RequiredValue(row, "assignment_method");
            var status = RequiredValue(row, "assignment_status");
            var confidence = ParseDouble(GetValue(row, "assignment_confidence"));
            var documentContentId = ParseInt(GetValue(row, "ParliamentDocumentContentId"));
            if (!documentContentId.HasValue || !documentContentIdSet.Contains(documentContentId.Value))
            {
                documentContentId = null;
            }

            currentAssignments.TryGetValue(projectLawId.Value, out var current);
            if (current is not null &&
               IsSameSeedAssignment(current, subtopic.Id, documentContentId, method, status, confidence))
            {
                counts.Skipped++;
                continue;
            }

            if (current is not null)
            {
                current.IsCurrent = false;
                current.UpdatedAtUtc = now;
                await _context.SaveChangesAsync(cancellationToken);
                counts.Updated++;
            }
            else
            {
                counts.Inserted++;
            }

            var assignment = new ProjectLawTopicAssignment
            {
                ProjectLawId = projectLawId.Value,
                ParliamentDocumentContentId = documentContentId,
                TaxonomyVersionId = taxonomyVersion.Id,
                SubtopicId = subtopic.Id,
                BestSubtopicId = subtopic.Id,
                AssignmentMethod = method,
                AssignmentStatus = status,
                AssignmentConfidence = confidence,
                EmbeddingProvider = taxonomyVersion.EmbeddingProvider,
                EmbeddingModel = taxonomyVersion.EmbeddingModel,
                IsCurrent = true,
                AssignedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _context.ProjectLawTopicAssignments.Add(assignment);
            currentAssignments[projectLawId.Value] = assignment;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return counts;
    }

    private static bool UpdateParent(ProposalTopicParent parent, ProposalTopicArtifactParent topic)
    {
        var changed = false;
        changed |= SetIfChanged(parent.Label, topic.Label, value => parent.Label = value);
        changed |= SetIfChanged(parent.Description, NullIfWhiteSpace(topic.Description), value => parent.Description = value);
        changed |= SetIfChanged(parent.DisplayOrder, topic.DisplayOrder, value => parent.DisplayOrder = value);
        changed |= SetIfChanged(
            parent.SourceClusterIdsJson,
            JsonSerializer.Serialize(
                topic.Subtopics
                    .SelectMany(x => x.SourceClusterIds)
                    .Select(ReadSourceClusterId)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal),
                JsonOptions),
            value => parent.SourceClusterIdsJson = value);
        return changed;
    }

    private static bool UpdateSubtopic(
        ProposalSubtopic subtopic,
        int parentTopicId,
        ProposalTopicArtifactSubtopic artifact,
        int displayOrder,
        ProposalTopicClassifierSubtopic? classifierSubtopic)
    {
        var changed = false;
        changed |= SetIfChanged(subtopic.ParentTopicId, parentTopicId, value => subtopic.ParentTopicId = value);
        changed |= SetIfChanged(subtopic.Label, artifact.Label, value => subtopic.Label = value);
        changed |= SetIfChanged(subtopic.Description, NullIfWhiteSpace(artifact.Description), value => subtopic.Description = value);
        changed |= SetIfChanged(subtopic.DisplayOrder, displayOrder, value => subtopic.DisplayOrder = value);
        changed |= SetIfChanged(
            subtopic.SourceClusterIdsJson,
            JsonSerializer.Serialize(
                artifact.SourceClusterIds
                    .Select(ReadSourceClusterId)
                    .OrderBy(x => x, StringComparer.Ordinal),
                JsonOptions),
            value => subtopic.SourceClusterIdsJson = value);

        if (classifierSubtopic is not null)
        {
            changed |= SetIfChanged(subtopic.CentroidIndex, classifierSubtopic.CentroidIndex, value => subtopic.CentroidIndex = value);
            changed |= SetIfChanged(subtopic.SupportCount, classifierSubtopic.SupportCount, value => subtopic.SupportCount = value);
            changed |= SetIfChanged(subtopic.SupportQuality, classifierSubtopic.SupportQuality, value => subtopic.SupportQuality = value);
            changed |= SetIfChanged(subtopic.SimilarityMin, classifierSubtopic.SimilarityMin, value => subtopic.SimilarityMin = value);
            changed |= SetIfChanged(subtopic.SimilarityP05, classifierSubtopic.SimilarityP05, value => subtopic.SimilarityP05 = value);
            changed |= SetIfChanged(subtopic.SimilarityP10, classifierSubtopic.SimilarityP10, value => subtopic.SimilarityP10 = value);
            changed |= SetIfChanged(subtopic.SimilarityP25, classifierSubtopic.SimilarityP25, value => subtopic.SimilarityP25 = value);
            changed |= SetIfChanged(subtopic.SimilarityMedian, classifierSubtopic.SimilarityMedian, value => subtopic.SimilarityMedian = value);
            changed |= SetIfChanged(subtopic.SimilarityMean, classifierSubtopic.SimilarityMean, value => subtopic.SimilarityMean = value);
            changed |= SetIfChanged(
                subtopic.RecommendedAutoThreshold,
                classifierSubtopic.RecommendedAutoThreshold,
                value => subtopic.RecommendedAutoThreshold = value);
        }

        return changed;
    }

    private static bool IsSameSeedAssignment(
        ProjectLawTopicAssignment assignment,
        int subtopicId,
        int? documentContentId,
        string method,
        string status,
        double? confidence)
    {
        return assignment.SubtopicId == subtopicId &&
               assignment.BestSubtopicId == subtopicId &&
               assignment.ParliamentDocumentContentId == documentContentId &&
               string.Equals(assignment.AssignmentMethod, method, StringComparison.Ordinal) &&
               string.Equals(assignment.AssignmentStatus, status, StringComparison.Ordinal) &&
               NullableDoubleEquals(assignment.AssignmentConfidence, confidence);
    }

    private static async Task<T> LoadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"Artifact '{path}' is empty or invalid JSON.");
    }

    private static List<Dictionary<string, string>> ReadCsv(string path)
    {
        using var parser = new TextFieldParser(path);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;

        var headers = parser.ReadFields()
                      ?? throw new InvalidOperationException($"CSV artifact '{path}' does not contain a header row.");
        var rows = new List<Dictionary<string, string>>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null)
            {
                continue;
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length && i < fields.Length; i++)
            {
                row[headers[i]] = fields[i];
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string? GetValue(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value : null;
    }

    private static string RequiredValue(Dictionary<string, string> row, string key)
    {
        var value = GetValue(row, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Seed assignment row is missing '{key}'.");
        }

        return value.Trim();
    }

    private static int? ParseInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static double? ParseDouble(string? value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ReadSourceClusterId(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => value.GetRawText()
        };
    }

    private static bool SetIfChanged<T>(T current, T next, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return false;
        }

        setter(next);
        return true;
    }

    private static bool NullableDoubleEquals(double? left, double? right)
    {
        return (!left.HasValue && !right.HasValue) ||
               (left.HasValue && right.HasValue && Math.Abs(left.Value - right.Value) < 0.000000001);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record UpsertCounts(int Read)
    {
        public int Inserted { get; set; }

        public int Updated { get; set; }
    }

    private sealed record SeedAssignmentCounts(int Read)
    {
        public int Inserted { get; set; }

        public int Updated { get; set; }

        public int Skipped { get; set; }

        public int MissingProjectLaws { get; set; }

        public int MissingSubtopics { get; set; }
    }
}
