using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Topics;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services.ProposalTopics;

public class ProjectLawTopicAssignmentService : IProjectLawTopicAssignmentService
{
    private const string AutoAssignedMethod = "centroid_auto_assigned";
    private const string NeedsReviewMethod = "centroid_needs_review";
    private const string UnassignedMethod = "unassigned";
    private const string AssignedStatus = "assigned";
    private const string NeedsReviewStatus = "needs_review";
    private const string UnassignedStatus = "unassigned";

    private readonly DatabaseContext _context;
    private readonly ITopicEmbeddingClient _embeddingClient;
    private readonly ProposalTopicOptions _options;
    private readonly ILogger<ProjectLawTopicAssignmentService> _logger;
    private ProposalTopicClassifierArtifact? _classifier;

    public ProjectLawTopicAssignmentService(
        DatabaseContext context,
        ITopicEmbeddingClient embeddingClient,
        IOptions<ProposalTopicOptions> options,
        ILogger<ProjectLawTopicAssignmentService> logger)
    {
        _context = context;
        _embeddingClient = embeddingClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ProjectLawTopicAssignmentRunResult> AssignMissingAsync(
        ProjectLawTopicAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var taxonomyVersion = await _context.ProposalTopicTaxonomyVersions
            .SingleOrDefaultAsync(x => x.Version == _options.TaxonomyVersion, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Proposal topic taxonomy '{_options.TaxonomyVersion}' has not been imported.");

        var classifier = await LoadClassifierAsync(cancellationToken);
        var subtopics = await _context.ProposalSubtopics
            .Where(x => x.TaxonomyVersionId == taxonomyVersion.Id)
            .ToDictionaryAsync(x => x.Slug, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var query = _context.ParliamentDocumentContents
            .Include(x => x.ProjectLaw)
            .Where(x => x.RedactionStatus == "Succeeded");

        if (request.ProjectLawId.HasValue)
        {
            query = query.Where(x => x.ProjectLawId == request.ProjectLawId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Legislature))
        {
            query = query.Where(x => x.ProjectLaw != null && x.ProjectLaw.Legislatura == request.Legislature);
        }

        query = query.OrderBy(x => x.Id);

        if (request.MaxDocuments is > 0)
        {
            query = query.Take(request.MaxDocuments.Value);
        }

        var documents = await query.ToListAsync(cancellationToken);
        var created = 0;
        var skipped = 0;
        var failed = 0;
        var autoAssigned = 0;
        var needsReview = 0;
        var unassigned = 0;
        var missingText = 0;
        var preservedReviewed = 0;

        foreach (var document in documents)
        {
            try
            {
                var result = await ProcessDocumentAsync(
                    document,
                    taxonomyVersion,
                    classifier,
                    subtopics,
                    request.Force,
                    cancellationToken);

                if (result.Created)
                {
                    created++;
                }
                else
                {
                    skipped++;
                }

                if (result.PreservedReviewed)
                {
                    preservedReviewed++;
                }

                if (result.MissingRedactedText)
                {
                    missingText++;
                }

                switch (result.AssignmentStatus)
                {
                    case AssignedStatus:
                        autoAssigned++;
                        break;
                    case NeedsReviewStatus:
                        needsReview++;
                        break;
                    case UnassignedStatus:
                        unassigned++;
                        break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failed++;
                DetachTopicAssignments(document.ProjectLawId);
                _logger.LogError(
                    ex,
                    "Failed to assign proposal topic for ProjectLaw {ProjectLawId} DocumentContent {DocumentContentId}.",
                    document.ProjectLawId,
                    document.Id);
            }
        }

        return new ProjectLawTopicAssignmentRunResult(
            documents.Count,
            created,
            skipped,
            failed,
            autoAssigned,
            needsReview,
            unassigned,
            missingText,
            preservedReviewed);
    }

    private async Task<AssignmentProcessResult> ProcessDocumentAsync(
        ParliamentDocumentContent document,
        ProposalTopicTaxonomyVersion taxonomyVersion,
        ProposalTopicClassifierArtifact classifier,
        IReadOnlyDictionary<string, ProposalSubtopic> subtopics,
        bool force,
        CancellationToken cancellationToken)
    {
        var existing = await FindCurrentAssignmentAsync(document.ProjectLawId, taxonomyVersion.Id, cancellationToken);
        if (existing is not null && IsReviewedOrSeededAssignment(existing))
        {
            _logger.LogInformation(
                "Preserving reviewed proposal topic assignment {AssignmentId} for ProjectLaw {ProjectLawId}.",
                existing.Id,
                document.ProjectLawId);
            return new AssignmentProcessResult(false, existing.AssignmentStatus, false, true);
        }

        if (!force &&
            existing is not null &&
            string.Equals(existing.SourceDocumentHash, document.RedactedContentHash, StringComparison.Ordinal))
        {
            return new AssignmentProcessResult(false, existing.AssignmentStatus, false, false);
        }

        var normalizedText = NormalizeText(document.RedactedContentText);
        var words = SplitWords(normalizedText);
        if (words.Count < _options.MinimumRedactedWordCount)
        {
            var created = await StoreAssignmentAsync(
                document,
                taxonomyVersion,
                existing,
                subtopicId: null,
                bestSubtopicId: null,
                secondBestSubtopicId: null,
                method: UnassignedMethod,
                status: UnassignedStatus,
                assignmentConfidence: null,
                bestSimilarity: null,
                secondBestSimilarity: null,
                similarityMargin: null,
                cancellationToken);

            return new AssignmentProcessResult(created, UnassignedStatus, true, false);
        }

        var chunks = BuildWordChunks(words, _options.ChunkWordCount, _options.ChunkOverlapWordCount);
        var chunkEmbeddings = await _embeddingClient.GenerateEmbeddingsAsync(chunks, cancellationToken);
        var proposalVector = MeanPoolAndNormalize(chunkEmbeddings);
        var match = Classify(proposalVector, classifier);
        var bestSubtopic = ResolveSubtopic(subtopics, match.Best.Subtopic.SubtopicSlug);
        var secondBestSubtopic = match.Second is null
            ? null
            : ResolveSubtopic(subtopics, match.Second.Subtopic.SubtopicSlug);

        var requiredThreshold = Math.Max(
            classifier.ClassificationPolicy.AutoSimilarityThreshold,
            match.Best.Subtopic.RecommendedAutoThreshold);
        if (string.Equals(match.Best.Subtopic.SupportQuality, "low", StringComparison.OrdinalIgnoreCase))
        {
            requiredThreshold = Math.Max(requiredThreshold, classifier.ClassificationPolicy.LowSupportAutoThreshold);
        }

        var method = UnassignedMethod;
        var status = UnassignedStatus;
        int? assignedSubtopicId = null;
        if (match.Best.Similarity >= requiredThreshold &&
            match.Margin >= classifier.ClassificationPolicy.MinSimilarityMargin)
        {
            method = AutoAssignedMethod;
            status = AssignedStatus;
            assignedSubtopicId = bestSubtopic.Id;
        }
        else if (match.Best.Similarity >= classifier.ClassificationPolicy.ReviewSimilarityThreshold)
        {
            method = NeedsReviewMethod;
            status = NeedsReviewStatus;
            assignedSubtopicId = bestSubtopic.Id;
        }

        var wasCreated = await StoreAssignmentAsync(
            document,
            taxonomyVersion,
            existing,
            assignedSubtopicId,
            bestSubtopic.Id,
            secondBestSubtopic?.Id,
            method,
            status,
            match.Best.Similarity,
            match.Best.Similarity,
            match.Second?.Similarity,
            match.Margin,
            cancellationToken);

        return new AssignmentProcessResult(wasCreated, status, false, false);
    }

    private async Task<bool> StoreAssignmentAsync(
        ParliamentDocumentContent document,
        ProposalTopicTaxonomyVersion taxonomyVersion,
        ProjectLawTopicAssignment? existing,
        int? subtopicId,
        int? bestSubtopicId,
        int? secondBestSubtopicId,
        string method,
        string status,
        double? assignmentConfidence,
        double? bestSimilarity,
        double? secondBestSimilarity,
        double? similarityMargin,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (existing is not null)
        {
            existing.IsCurrent = false;
            existing.UpdatedAtUtc = now;
            await _context.SaveChangesAsync(cancellationToken);
        }

        var assignment = new ProjectLawTopicAssignment
        {
            ProjectLawId = document.ProjectLawId,
            ParliamentDocumentContentId = document.Id,
            TaxonomyVersionId = taxonomyVersion.Id,
            SubtopicId = subtopicId,
            BestSubtopicId = bestSubtopicId,
            SecondBestSubtopicId = secondBestSubtopicId,
            AssignmentMethod = method,
            AssignmentStatus = status,
            AssignmentConfidence = assignmentConfidence,
            BestSimilarity = bestSimilarity,
            SecondBestSimilarity = secondBestSimilarity,
            SimilarityMargin = similarityMargin,
            EmbeddingProvider = _embeddingClient.Provider,
            EmbeddingModel = _embeddingClient.ModelName,
            SourceDocumentHash = document.RedactedContentHash,
            IsCurrent = true,
            AssignedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.ProjectLawTopicAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stored proposal topic assignment for ProjectLaw {ProjectLawId}. Status={Status} Method={Method} BestSimilarity={BestSimilarity} Margin={Margin}.",
            document.ProjectLawId,
            status,
            method,
            bestSimilarity,
            similarityMargin);

        return true;
    }

    private Task<ProjectLawTopicAssignment?> FindCurrentAssignmentAsync(
        int projectLawId,
        int taxonomyVersionId,
        CancellationToken cancellationToken)
    {
        return _context.ProjectLawTopicAssignments
            .Where(x => x.ProjectLawId == projectLawId)
            .Where(x => x.TaxonomyVersionId == taxonomyVersionId)
            .Where(x => x.IsCurrent)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<ProposalTopicClassifierArtifact> LoadClassifierAsync(CancellationToken cancellationToken)
    {
        if (_classifier is not null)
        {
            return _classifier;
        }

        var path = _options.ResolveArtifactPath(_options.ClassifierFileName);
        await using var stream = File.OpenRead(path);
        _classifier = await JsonSerializer.DeserializeAsync<ProposalTopicClassifierArtifact>(
            stream,
            JsonOptions,
            cancellationToken)
            ?? throw new InvalidOperationException($"Classifier artifact '{path}' is empty or invalid JSON.");

        foreach (var subtopic in _classifier.Subtopics)
        {
            subtopic.Centroid = NormalizeVector(subtopic.Centroid);
        }

        return _classifier;
    }

    private static ClassificationMatch Classify(
        IReadOnlyList<double> proposalVector,
        ProposalTopicClassifierArtifact classifier)
    {
        var scored = classifier.Subtopics
            .Select(subtopic => new ScoredSubtopic(subtopic, Dot(proposalVector, subtopic.Centroid)))
            .OrderByDescending(x => x.Similarity)
            .ToList();

        if (scored.Count == 0)
        {
            throw new InvalidOperationException("Classifier artifact does not contain any subtopic centroids.");
        }

        var best = scored[0];
        var second = scored.Count > 1 ? scored[1] : null;
        var margin = best.Similarity - (second?.Similarity ?? 0d);
        return new ClassificationMatch(best, second, margin);
    }

    private static ProposalSubtopic ResolveSubtopic(
        IReadOnlyDictionary<string, ProposalSubtopic> subtopics,
        string slug)
    {
        return subtopics.TryGetValue(slug, out var subtopic)
            ? subtopic
            : throw new InvalidOperationException($"Classifier references unknown proposal subtopic '{slug}'.");
    }

    private static IReadOnlyList<string> BuildWordChunks(
        IReadOnlyList<string> words,
        int chunkWordCount,
        int chunkOverlapWordCount)
    {
        var chunkSize = Math.Max(1, chunkWordCount);
        var overlap = Math.Clamp(chunkOverlapWordCount, 0, chunkSize - 1);
        var step = chunkSize - overlap;
        var chunks = new List<string>();

        for (var start = 0; start < words.Count; start += step)
        {
            chunks.Add(string.Join(" ", words.Skip(start).Take(chunkSize)));
            if (start + chunkSize >= words.Count)
            {
                break;
            }
        }

        return chunks;
    }

    private static IReadOnlyList<double> MeanPoolAndNormalize(IReadOnlyList<IReadOnlyList<double>> embeddings)
    {
        if (embeddings.Count == 0)
        {
            throw new InvalidOperationException("Embedding client returned no embeddings.");
        }

        var dimensions = embeddings[0].Count;
        if (dimensions == 0)
        {
            throw new InvalidOperationException("Embedding client returned an empty embedding vector.");
        }

        var pooled = new double[dimensions];
        foreach (var embedding in embeddings)
        {
            if (embedding.Count != dimensions)
            {
                throw new InvalidOperationException("Embedding client returned vectors with inconsistent dimensions.");
            }

            for (var i = 0; i < dimensions; i++)
            {
                pooled[i] += embedding[i];
            }
        }

        for (var i = 0; i < pooled.Length; i++)
        {
            pooled[i] /= embeddings.Count;
        }

        return NormalizeVector(pooled);
    }

    private static double[] NormalizeVector(IReadOnlyList<double> vector)
    {
        var magnitude = Math.Sqrt(vector.Sum(x => x * x));
        if (magnitude <= 0)
        {
            throw new InvalidOperationException("Cannot normalize a zero-length embedding vector.");
        }

        return vector.Select(x => x / magnitude).ToArray();
    }

    private static double Dot(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        if (left.Count != right.Count)
        {
            throw new InvalidOperationException(
                $"Embedding dimensions do not match classifier centroid dimensions ({left.Count} != {right.Count}).");
        }

        var result = 0d;
        for (var i = 0; i < left.Count; i++)
        {
            result += left[i] * right[i];
        }

        return result;
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return WhitespaceRegex.Replace(value.Trim(), " ");
    }

    private static IReadOnlyList<string> SplitWords(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool IsReviewedOrSeededAssignment(ProjectLawTopicAssignment assignment)
    {
        return !IsAutomaticMethod(assignment.AssignmentMethod);
    }

    private static bool IsAutomaticMethod(string method)
    {
        return string.Equals(method, AutoAssignedMethod, StringComparison.Ordinal) ||
               string.Equals(method, NeedsReviewMethod, StringComparison.Ordinal) ||
               string.Equals(method, UnassignedMethod, StringComparison.Ordinal);
    }

    private void DetachTopicAssignments(int projectLawId)
    {
        var entries = _context.ChangeTracker
            .Entries<ProjectLawTopicAssignment>()
            .Where(x => x.Entity.ProjectLawId == projectLawId)
            .ToList();

        foreach (var entry in entries)
        {
            entry.State = EntityState.Detached;
        }
    }

    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record AssignmentProcessResult(
        bool Created,
        string? AssignmentStatus,
        bool MissingRedactedText,
        bool PreservedReviewed);

    private sealed record ClassificationMatch(
        ScoredSubtopic Best,
        ScoredSubtopic? Second,
        double Margin);

    private sealed record ScoredSubtopic(
        ProposalTopicClassifierSubtopic Subtopic,
        double Similarity);
}
