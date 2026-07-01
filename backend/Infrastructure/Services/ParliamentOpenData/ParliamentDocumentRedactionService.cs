using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Imports;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services.ParliamentOpenData;

public partial class ParliamentDocumentRedactionService : IParliamentDocumentRedactionService
{
    private readonly DatabaseContext _context;
    private readonly HttpClient _httpClient;
    private readonly IReadOnlyList<IDocumentExtractor> _extractors;
    private readonly IDocumentModelRedactor _redactor;
    private readonly IDocumentModelRenderer _renderer;
    private readonly ILogger<ParliamentDocumentRedactionService> _logger;

    public ParliamentDocumentRedactionService(
        DatabaseContext context,
        HttpClient httpClient,
        IEnumerable<IDocumentExtractor> extractors,
        IDocumentModelRedactor redactor,
        IDocumentModelRenderer renderer,
        ILogger<ParliamentDocumentRedactionService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _extractors = extractors.ToList();
        _redactor = redactor;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task<ParliamentDocumentRedactionResult> ProcessInitiativeTextDocumentsAsync(
        string? legislature,
        int? projectLawId,
        int maxDocuments,
        bool forceUpsert = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ParliamentInitiativeDocuments
            .Include(x => x.Content)
            .Include(x => x.ProjectLaw)
            .Where(x => x.Scope == "InitiativeText")
            .Where(x => x.ProjectLaw != null)
            .Where(x => !string.IsNullOrWhiteSpace(x.ProjectLaw!.FullProposalTextLink))
            .Where(x => x.Url == x.ProjectLaw!.FullProposalTextLink);

        if (!string.IsNullOrWhiteSpace(legislature))
        {
            query = query.Where(x => x.ProjectLaw!.Legislatura == legislature);
        }

        if (projectLawId.HasValue)
        {
            query = query.Where(x => x.ProjectLawId == projectLawId.Value);
        }

        var documents = await query
            .OrderBy(x => x.Id)
            .Take(Math.Max(1, maxDocuments))
            .ToListAsync(cancellationToken);

        var processed = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var document in documents)
        {
            try
            {
                if (await ProcessDocumentAsync(document, forceUpsert, cancellationToken))
                {
                    processed++;
                }
                else
                {
                    skipped++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                await MarkFailedAsync(document, ex.Message, cancellationToken);
                _logger.LogError(
                    ex,
                    "Failed to extract/redact document {DocumentId} for ProjectLaw {ProjectLawId}.",
                    document.Id,
                    document.ProjectLawId);
            }
        }

        return new ParliamentDocumentRedactionResult(documents.Count, processed, skipped, failed);
    }

    private async Task<bool> ProcessDocumentAsync(
        ParliamentInitiativeDocument document,
        bool forceUpsert,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(document.Url))
        {
            throw new InvalidOperationException("Document URL is missing.");
        }

        var bytes = await ReadDocumentBytesAsync(document.Url, cancellationToken);
        var sourceHash = ComputeSha256(bytes);
        var content = document.Content ?? new ParliamentDocumentContent
        {
            ProjectLawId = document.ProjectLawId,
            ParliamentInitiativeDocumentId = document.Id,
            SourceUrl = document.Url
        };

        if (document.Content is null)
        {
            _context.ParliamentDocumentContents.Add(content);
        }

        var sourceName = TryGetFileName(document.Url);
        var extractor = ResolveExtractor(bytes, sourceName);

        if (!forceUpsert &&
            content.SourceContentHash == sourceHash &&
            content.ExtractorVersion == extractor.ExtractorVersion &&
            content.RedactionPolicyVersion == _redactor.PolicyVersion &&
            content.RendererVersion == _renderer.RendererVersion &&
            content.ExtractionStatus == "Succeeded" &&
            content.RedactionStatus == "Succeeded")
        {
            _logger.LogInformation(
                "Skipping document {DocumentId} for ProjectLaw {ProjectLawId}; redacted content is current. Use force-upsert to rewrite it.",
                document.Id,
                document.ProjectLawId);
            return false;
        }

        if (forceUpsert && content.Id != 0)
        {
            _logger.LogInformation(
                "Force-upserting redacted document content for DocumentId={DocumentId} ProjectLawId={ProjectLawId}.",
                document.Id,
                document.ProjectLawId);
        }

        var extracted = await extractor.ExtractAsync(bytes, sourceName, cancellationToken);
        var terms = await LoadTermsAsync(document.ProjectLawId, cancellationToken);
        var redactedModel = _redactor.Redact(extracted.Document, terms);
        var redactedModelJson = JsonSerializer.Serialize(redactedModel, JsonOptions);
        var rendered = _renderer.Render(redactedModel);

        content.SourceUrl = document.Url;
        content.SourceContentHash = sourceHash;
        content.SourceContentLength = bytes.Length;
        content.ExtractedContentHash = ComputeSha256(redactedModelJson);
        content.RedactedContentHash = ComputeSha256(rendered.PlainText + "\n" + rendered.Html);
        content.RedactedDocumentModelJson = redactedModelJson;
        content.RedactedContentText = rendered.PlainText;
        content.RedactedContentHtml = rendered.Html;
        content.ExtractionStatus = "Succeeded";
        content.RedactionStatus = "Succeeded";
        content.ExtractorKind = extracted.ExtractorKind;
        content.ExtractorVersion = extracted.ExtractorVersion;
        content.RendererVersion = rendered.RendererVersion;
        content.DocumentModelSchemaVersion = redactedModel.SchemaVersion;
        content.RedactionPolicyVersion = _redactor.PolicyVersion;
        content.ErrorMessage = null;
        content.ExtractedAtUtc = DateTime.UtcNow;
        content.RedactedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IDocumentExtractor ResolveExtractor(byte[] bytes, string sourceName)
    {
        var extractor = _extractors.FirstOrDefault(x => !x.CanExtract(sourceName) && x.CanExtract(bytes, sourceName))
                        ?? _extractors.FirstOrDefault(x => x.CanExtract(bytes, sourceName))
                        ?? throw new NotSupportedException($"No document extractor is registered for '{sourceName}'.");

        if (!extractor.CanExtract(sourceName))
        {
            _logger.LogInformation(
                "Selected document extractor {ExtractorKind} for {SourceName} by byte signature instead of file name.",
                extractor.ExtractorKind,
                sourceName);
        }

        return extractor;
    }

    private async Task<IReadOnlyList<string>> LoadTermsAsync(
        int projectLawId,
        CancellationToken cancellationToken)
    {
        var projectLaw = await _context.ProjectLaws
            .Include(x => x.ImportedAuthors)
            .Include(x => x.ProposingParty)
            .SingleAsync(x => x.Id == projectLawId, cancellationToken);

        var terms = await _context.ParliamentRedactionTerms
            .Where(x => x.Legislature == projectLaw.Legislatura)
            .Select(x => x.Term)
            .ToListAsync(cancellationToken);

        foreach (var author in projectLaw.ImportedAuthors)
        {
            AddTerm(terms, author.Name);
            AddTerm(terms, author.Acronym);
        }

        AddTerm(terms, projectLaw.ProposingParty?.partyAcronym);
        AddTerm(terms, projectLaw.ProposingParty?.fullName);

        return terms
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddTerm(List<string> terms, string? term)
    {
        if (!string.IsNullOrWhiteSpace(term))
        {
            terms.Add(term.Trim());
        }
    }

    private async Task<byte[]> ReadDocumentBytesAsync(string url, CancellationToken cancellationToken)
    {
        if (File.Exists(url))
        {
            return await File.ReadAllBytesAsync(url, cancellationToken);
        }

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task MarkFailedAsync(
        ParliamentInitiativeDocument document,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var content = document.Content ?? new ParliamentDocumentContent
        {
            ProjectLawId = document.ProjectLawId,
            ParliamentInitiativeDocumentId = document.Id,
            SourceUrl = document.Url
        };

        if (document.Content is null)
        {
            _context.ParliamentDocumentContents.Add(content);
        }

        content.ExtractionStatus = "Failed";
        content.RedactionStatus = "Failed";
        content.ErrorMessage = errorMessage;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string TryGetFileName(string sourceUrl)
    {
        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            var query = ParseQuery(uri.Query);
            if (query.TryGetValue("fich", out var fileName) && !string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }

            return Path.GetFileName(uri.LocalPath);
        }

        return Path.GetFileName(sourceUrl);
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        return query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('=', 2))
            .Where(x => x.Length == 2)
            .ToDictionary(
                x => WebUtility.UrlDecode(x[0]),
                x => WebUtility.UrlDecode(x[1]),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string ComputeSha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string ComputeSha256(string value)
    {
        return ComputeSha256(Encoding.UTF8.GetBytes(value));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };
}
