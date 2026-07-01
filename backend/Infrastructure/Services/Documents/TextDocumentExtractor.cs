using System.Text;
using System.Text.RegularExpressions;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Documents;
using Parlamento.Domain.Documents;

namespace Parlamento.Infrastructure.Services.Documents;

public partial class TextDocumentExtractor : IDocumentExtractor
{
    public string ExtractorKind => "Text";

    public string ExtractorVersion => "text-document-extractor-v1";

    public bool CanExtract(string sourceName)
    {
        return true;
    }

    public Task<DocumentExtractionResult> ExtractAsync(
        byte[] bytes,
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        var text = Encoding.UTF8.GetString(bytes);
        var page = new ParliamentDocumentPage { PageNumber = 1 };

        foreach (var paragraph in ParagraphSplitRegex()
                     .Split(text)
                     .Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            page.Blocks.Add(new ParliamentDocumentBlock
            {
                Kind = ParliamentDocumentBlockKind.Paragraph,
                Runs = BuildRuns(paragraph.Trim())
            });
        }

        var document = new ParliamentDocumentModel
        {
            SourceKind = "Text",
            Pages = [page]
        };

        return Task.FromResult(new DocumentExtractionResult(
            document,
            ExtractorKind,
            ExtractorVersion));
    }

    private static List<ParliamentDocumentRun> BuildRuns(string text)
    {
        var runs = new List<ParliamentDocumentRun>();
        var lines = text.Split(
            ["\r\n", "\n"],
            StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                runs.Add(new ParliamentDocumentRun { Kind = ParliamentDocumentRunKind.LineBreak });
            }

            runs.Add(new ParliamentDocumentRun
            {
                Kind = ParliamentDocumentRunKind.Text,
                Text = lines[i]
            });
        }

        return runs;
    }

    [GeneratedRegex(@"\r?\n\s*\r?\n+")]
    private static partial Regex ParagraphSplitRegex();
}
