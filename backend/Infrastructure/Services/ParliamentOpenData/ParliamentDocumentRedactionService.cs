using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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
    private readonly ILogger<ParliamentDocumentRedactionService> _logger;

    public ParliamentDocumentRedactionService(
        DatabaseContext context,
        HttpClient httpClient,
        ILogger<ParliamentDocumentRedactionService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ParliamentDocumentRedactionResult> ProcessInitiativeTextDocumentsAsync(
        string? legislature,
        int? projectLawId,
        int maxDocuments,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ParliamentInitiativeDocuments
            .Include(x => x.Content)
            .Where(x => x.Scope == "InitiativeText");

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
                if (await ProcessDocumentAsync(document, cancellationToken))
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

        if (content.SourceContentHash == sourceHash &&
            content.ExtractionStatus == "Succeeded" &&
            content.RedactionStatus == "Succeeded")
        {
            return false;
        }

        var extracted = Extract(bytes, document.Url);
        var terms = await LoadTermsAsync(document.ProjectLawId, cancellationToken);
        var redactedText = RedactionTextService.RedactText(extracted.PlainText, terms);
        var redactedHtml = RedactionTextService.PlainTextToHtml(redactedText);

        content.SourceUrl = document.Url;
        content.SourceContentHash = sourceHash;
        content.SourceContentLength = bytes.Length;
        content.ExtractedContentHash = ComputeSha256(extracted.PlainText);
        content.RedactedContentHash = ComputeSha256(redactedText);
        content.RedactedContentText = redactedText;
        content.RedactedContentHtml = redactedHtml;
        content.ExtractionStatus = "Succeeded";
        content.RedactionStatus = "Succeeded";
        content.ExtractorKind = extracted.ExtractorKind;
        content.RedactionPolicyVersion = RedactionTextService.PolicyVersion;
        content.ErrorMessage = null;
        content.ExtractedAtUtc = DateTime.UtcNow;
        content.RedactedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
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

    private static ExtractedDocument Extract(byte[] bytes, string sourceUrl)
    {
        var fileName = TryGetFileName(sourceUrl).ToLowerInvariant();

        if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractDocx(bytes);
        }

        if (fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
        {
            var html = Encoding.UTF8.GetString(bytes);
            var text = WebUtility.HtmlDecode(TagRegex().Replace(html, " "));
            return new ExtractedDocument(NormalizeWhitespace(text), "Html");
        }

        if (fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return new ExtractedDocument(Encoding.UTF8.GetString(bytes), "Text");
        }

        if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "PDF extraction is not implemented yet. Add a PDF extractor library or external tool before processing PDF-only initiative texts.");
        }

        return new ExtractedDocument(Encoding.UTF8.GetString(bytes), "Utf8Fallback");
    }

    private static ExtractedDocument ExtractDocx(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = archive.GetEntry("word/document.xml")
                    ?? throw new InvalidOperationException("DOCX file does not contain word/document.xml.");

        using var entryStream = entry.Open();
        var document = XDocument.Load(entryStream);
        var paragraphs = document
            .Descendants()
            .Where(x => x.Name.LocalName == "p")
            .Select(x => string.Concat(x.Descendants().Where(y => y.Name.LocalName == "t").Select(y => y.Value)).Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return new ExtractedDocument(string.Join(Environment.NewLine + Environment.NewLine, paragraphs), "Docx");
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

    private static string NormalizeWhitespace(string value)
    {
        return WhitespaceRegex().Replace(value, " ").Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    private record ExtractedDocument(string PlainText, string ExtractorKind);
}
