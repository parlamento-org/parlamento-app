using System.Net;
using System.Text.RegularExpressions;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Documents;
using Parlamento.Domain.Documents;

namespace Parlamento.Infrastructure.Services.Documents;

public partial class HtmlDocumentExtractor : IDocumentExtractor
{
    public string ExtractorKind => "Html";

    public string ExtractorVersion => "html-document-extractor-v1";

    public bool CanExtract(string sourceName)
    {
        return sourceName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
               sourceName.EndsWith(".htm", StringComparison.OrdinalIgnoreCase);
    }

    public Task<DocumentExtractionResult> ExtractAsync(
        byte[] bytes,
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        var html = System.Text.Encoding.UTF8.GetString(bytes);
        var page = new ParliamentDocumentPage { PageNumber = 1 };

        foreach (Match match in BlockRegex().Matches(html))
        {
            var tag = match.Groups["tag"].Value.ToLowerInvariant();
            var body = match.Groups["body"].Value;
            var text = NormalizeText(StripTags(body));
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            page.Blocks.Add(new ParliamentDocumentBlock
            {
                Kind = tag.StartsWith('h') ? ParliamentDocumentBlockKind.Heading : ParliamentDocumentBlockKind.Paragraph,
                HeadingLevel = tag.StartsWith('h') && int.TryParse(tag[1..], out var level) ? level : null,
                Runs = BuildInlineRuns(body)
            });
        }

        if (page.Blocks.Count == 0)
        {
            var text = NormalizeText(StripTags(html));
            if (!string.IsNullOrWhiteSpace(text))
            {
                page.Blocks.Add(new ParliamentDocumentBlock
                {
                    Kind = ParliamentDocumentBlockKind.Paragraph,
                    Runs = [new ParliamentDocumentRun { Text = text }]
                });
            }
        }

        var document = new ParliamentDocumentModel
        {
            SourceKind = "Html",
            Pages = [page]
        };

        return Task.FromResult(new DocumentExtractionResult(
            document,
            ExtractorKind,
            ExtractorVersion));
    }

    private static List<ParliamentDocumentRun> BuildInlineRuns(string html)
    {
        var runs = new List<ParliamentDocumentRun>();
        var bold = false;
        var italic = false;
        var underline = false;
        string? href = null;
        var cursor = 0;

        foreach (Match tagMatch in TagRegex().Matches(html))
        {
            AppendTextRun(html[cursor..tagMatch.Index], runs, bold, italic, underline, href);

            var tag = tagMatch.Value;
            var tagName = TagNameRegex().Match(tag).Groups["name"].Value.ToLowerInvariant();
            var closing = tag.StartsWith("</", StringComparison.Ordinal);

            switch (tagName)
            {
                case "strong":
                case "b":
                    bold = !closing;
                    break;
                case "em":
                case "i":
                    italic = !closing;
                    break;
                case "u":
                    underline = !closing;
                    break;
                case "a":
                    href = closing ? null : TryExtractHref(tag);
                    break;
                case "br":
                    runs.Add(new ParliamentDocumentRun { Kind = ParliamentDocumentRunKind.LineBreak });
                    break;
            }

            cursor = tagMatch.Index + tagMatch.Length;
        }

        AppendTextRun(html[cursor..], runs, bold, italic, underline, href);
        return runs.Count == 0
            ? [new ParliamentDocumentRun { Text = NormalizeText(StripTags(html)) }]
            : runs;
    }

    private static void AppendTextRun(
        string raw,
        List<ParliamentDocumentRun> runs,
        bool bold,
        bool italic,
        bool underline,
        string? href)
    {
        var text = NormalizeText(WebUtility.HtmlDecode(raw));
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        runs.Add(new ParliamentDocumentRun
        {
            Kind = ParliamentDocumentRunKind.Text,
            Text = text,
            Bold = bold,
            Italic = italic,
            Underline = underline,
            Href = href
        });
    }

    private static string? TryExtractHref(string tag)
    {
        var match = HrefRegex().Match(tag);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["href"].Value) : null;
    }

    private static string StripTags(string value)
    {
        return TagRegex().Replace(value, " ");
    }

    private static string NormalizeText(string value)
    {
        return WhitespaceRegex().Replace(WebUtility.HtmlDecode(value), " ").Trim();
    }

    [GeneratedRegex(@"<(?<tag>h[1-6]|p|li|td|th|div)[^>]*>(?<body>.*?)</\k<tag>>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BlockRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"^</?\s*(?<name>[a-zA-Z0-9]+)")]
    private static partial Regex TagNameRegex();

    [GeneratedRegex("href\\s*=\\s*[\"'](?<href>.*?)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex HrefRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
