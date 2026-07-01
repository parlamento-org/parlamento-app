using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Documents;
using Parlamento.Domain.Documents;

namespace Parlamento.Infrastructure.Services.Documents;

public class ITextPdfDocumentExtractor : IDocumentExtractor
{
    public string ExtractorKind => "iText";

    public string ExtractorVersion => "itext-9.3.0-location-text-v2";

    public bool CanExtract(string sourceName)
    {
        return sourceName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public bool CanExtract(byte[] bytes, string sourceName)
    {
        return HasPdfSignature(bytes) || CanExtract(sourceName);
    }

    public Task<DocumentExtractionResult> ExtractAsync(
        byte[] bytes,
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        using var input = new MemoryStream(bytes);
        using var reader = new PdfReader(input);
        using var pdf = new PdfDocument(reader);

        var pages = new List<ParliamentDocumentPage>();
        for (var pageNumber = 1; pageNumber <= pdf.GetNumberOfPages(); pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pdfPage = pdf.GetPage(pageNumber);
            var pageSize = pdfPage.GetPageSize();
            var text = PdfTextExtractor.GetTextFromPage(
                pdfPage,
                new LocationTextExtractionStrategy());

            pages.Add(new ParliamentDocumentPage
            {
                PageNumber = pageNumber,
                Width = pageSize.GetWidth(),
                Height = pageSize.GetHeight(),
                Blocks = BuildBlocks(text)
            });
        }

        if (pages.Sum(x => x.Blocks.Count) == 0)
        {
            throw new NotSupportedException(
                "No selectable text was found in the PDF. Scanned PDFs require an OCR extraction pipeline.");
        }

        var document = new ParliamentDocumentModel
        {
            SourceKind = "Pdf",
            Pages = pages
        };

        return Task.FromResult(new DocumentExtractionResult(
            document,
            ExtractorKind,
            ExtractorVersion));
    }

    private static List<ParliamentDocumentBlock> BuildBlocks(string text)
    {
        var blocks = new List<ParliamentDocumentBlock>();
        foreach (var line in NormalizeTextLines(text))
        {
            blocks.Add(new ParliamentDocumentBlock
            {
                Kind = LooksLikeHeading(line)
                    ? ParliamentDocumentBlockKind.Heading
                    : ParliamentDocumentBlockKind.Paragraph,
                HeadingLevel = LooksLikeHeading(line) ? 2 : null,
                Runs =
                [
                    new ParliamentDocumentRun
                    {
                        Kind = ParliamentDocumentRunKind.Text,
                        Text = line,
                        WidthEm = Math.Clamp(line.Length * 0.56, 1.4, 80)
                    }
                ]
            });
        }

        return blocks;
    }

    public static IReadOnlyList<string> NormalizeTextLinesForTests(string text)
    {
        return NormalizeTextLines(text);
    }

    private static List<string> NormalizeTextLines(string text)
    {
        var lines = new List<string>();
        foreach (var rawLine in text.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var line = rawLine.Trim();
            if (IsStandaloneNumericLine(line))
            {
                MergeOrDiscardStandaloneNumber(lines, line);
                continue;
            }

            lines.Add(line);
        }

        return lines;
    }

    private static void MergeOrDiscardStandaloneNumber(List<string> lines, string number)
    {
        if (lines.Count == 0)
        {
            return;
        }

        var previous = lines[^1];
        if (LooksLikeFootnoteAnchor(previous) || !PreviousLineCanAcceptStandaloneNumber(previous))
        {
            return;
        }

        lines[^1] = previous + (NeedsSpaceBeforeNumber(previous) ? " " : string.Empty) + number;
    }

    private static bool IsStandaloneNumericLine(string line)
    {
        return line.Length <= 3 && line.All(char.IsDigit);
    }

    private static bool LooksLikeFootnoteAnchor(string previous)
    {
        if (string.IsNullOrWhiteSpace(previous))
        {
            return false;
        }

        var last = previous.TrimEnd()[^1];
        return char.IsDigit(last) ||
               last is '.' or ',' or ':' or ';' or ')' or ']' or '"' or '\'' or '»' or '”';
    }

    private static bool NeedsSpaceBeforeNumber(string previous)
    {
        return previous.Length > 0 && char.IsLetterOrDigit(previous.TrimEnd()[^1]);
    }

    private static bool PreviousLineCanAcceptStandaloneNumber(string previous)
    {
        var normalized = previous.TrimEnd().ToLowerInvariant();
        return normalized.EndsWith(" de", StringComparison.Ordinal) ||
               normalized.EndsWith(" em", StringComparison.Ordinal) ||
               normalized.EndsWith(" artigo", StringComparison.Ordinal) ||
               normalized.EndsWith(" numero", StringComparison.Ordinal) ||
               normalized.EndsWith(" número", StringComparison.Ordinal) ||
               normalized.EndsWith(" n.", StringComparison.Ordinal) ||
               normalized.EndsWith(" n.º", StringComparison.Ordinal) ||
               normalized.EndsWith(" nº", StringComparison.Ordinal);
    }

    private static bool LooksLikeHeading(string line)
    {
        return line.Length <= 120 &&
               (line.StartsWith("Projeto ", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("Exposição de motivos", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasPdfSignature(byte[] bytes)
    {
        return bytes.Length >= 4 &&
               bytes[0] == '%' &&
               bytes[1] == 'P' &&
               bytes[2] == 'D' &&
               bytes[3] == 'F';
    }
}
