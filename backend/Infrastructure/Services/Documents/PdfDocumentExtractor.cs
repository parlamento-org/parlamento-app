using System.Text.RegularExpressions;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Documents;
using Parlamento.Domain.Documents;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Parlamento.Infrastructure.Services.Documents;

public partial class PdfDocumentExtractor : IDocumentExtractor
{
    public string ExtractorKind => "PdfPig";

    public string ExtractorVersion => "pdfpig-document-extractor-v1";

    public bool CanExtract(string sourceName)
    {
        return sourceName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<DocumentExtractionResult> ExtractAsync(
        byte[] bytes,
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(bytes);
        using var pdf = PdfDocument.Open(stream);
        var pages = new List<ParliamentDocumentPage>();

        foreach (var pdfPage in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = new ParliamentDocumentPage
            {
                PageNumber = pdfPage.Number,
                Width = Convert.ToDouble(pdfPage.Width),
                Height = Convert.ToDouble(pdfPage.Height),
                Blocks = BuildPageBlocks(pdfPage)
            };

            pages.Add(page);
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

    private static List<ParliamentDocumentBlock> BuildPageBlocks(Page page)
    {
        var words = page.GetWords()
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .Select(x => new PdfWord(
                x.Text,
                Convert.ToDouble(x.BoundingBox.Left),
                Convert.ToDouble(x.BoundingBox.Bottom),
                Convert.ToDouble(x.BoundingBox.Right),
                Convert.ToDouble(x.BoundingBox.Top)))
            .OrderByDescending(x => x.Top)
            .ThenBy(x => x.Left)
            .ToList();

        if (words.Count == 0)
        {
            return [];
        }

        var lines = GroupLines(words);
        var medianHeight = Median(lines.Select(x => x.Height).Where(x => x > 0).ToList());
        var blocks = new List<ParliamentDocumentBlock>();
        var paragraphLines = new List<PdfLine>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.Text))
            {
                FlushParagraph(paragraphLines, blocks, medianHeight);
                continue;
            }

            if (IsStandaloneHeading(line, medianHeight))
            {
                FlushParagraph(paragraphLines, blocks, medianHeight);
                blocks.Add(ToBlock(line, ParliamentDocumentBlockKind.Heading, HeadingLevel(line, medianHeight)));
                continue;
            }

            if (StartsListItem(line.Text))
            {
                FlushParagraph(paragraphLines, blocks, medianHeight);
                blocks.Add(ToBlock(line, ParliamentDocumentBlockKind.Paragraph, null));
                continue;
            }

            var previous = paragraphLines.LastOrDefault();
            if (previous is not null && IsParagraphBreak(previous, line, medianHeight))
            {
                FlushParagraph(paragraphLines, blocks, medianHeight);
            }

            paragraphLines.Add(line);
        }

        FlushParagraph(paragraphLines, blocks, medianHeight);
        return blocks;
    }

    private static List<PdfLine> GroupLines(List<PdfWord> words)
    {
        var lines = new List<List<PdfWord>>();
        foreach (var word in words)
        {
            var line = lines.FirstOrDefault(existing =>
                Math.Abs(existing.Average(x => x.Baseline) - word.Baseline) <= Math.Max(2.5, word.Height * 0.45));

            if (line is null)
            {
                lines.Add([word]);
            }
            else
            {
                line.Add(word);
            }
        }

        return lines
            .Select(lineWords => lineWords.OrderBy(x => x.Left).ToList())
            .Select(lineWords => new PdfLine(
                string.Join(" ", lineWords.Select(x => x.Text)),
                lineWords.Min(x => x.Left),
                lineWords.Min(x => x.Bottom),
                lineWords.Max(x => x.Right),
                lineWords.Max(x => x.Top),
                lineWords.Average(x => x.Height)))
            .OrderByDescending(x => x.Top)
            .ThenBy(x => x.Left)
            .ToList();
    }

    private static bool IsStandaloneHeading(PdfLine line, double medianHeight)
    {
        if (line.Text.Length > 120)
        {
            return false;
        }

        return line.Height >= medianHeight * 1.18 ||
               UppercaseLetterRegex().IsMatch(line.Text);
    }

    private static int HeadingLevel(PdfLine line, double medianHeight)
    {
        if (line.Height >= medianHeight * 1.45)
        {
            return 1;
        }

        if (line.Height >= medianHeight * 1.25)
        {
            return 2;
        }

        return 3;
    }

    private static bool StartsListItem(string text)
    {
        return ListPrefixRegex().IsMatch(text);
    }

    private static bool IsParagraphBreak(PdfLine previous, PdfLine current, double medianHeight)
    {
        var verticalGap = previous.Bottom - current.Top;
        return verticalGap > Math.Max(4, medianHeight * 0.7) ||
               Math.Abs(previous.Left - current.Left) > 24;
    }

    private static void FlushParagraph(
        List<PdfLine> lines,
        List<ParliamentDocumentBlock> blocks,
        double medianHeight)
    {
        if (lines.Count == 0)
        {
            return;
        }

        var first = lines[0];
        var runs = new List<ParliamentDocumentRun>();
        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                runs.Add(new ParliamentDocumentRun { Kind = ParliamentDocumentRunKind.LineBreak });
            }

            runs.Add(new ParliamentDocumentRun
            {
                Kind = ParliamentDocumentRunKind.Text,
                Text = lines[i].Text,
                WidthEm = EstimateWidthEm(lines[i].Text)
            });
        }

        blocks.Add(new ParliamentDocumentBlock
        {
            Kind = ParliamentDocumentBlockKind.Paragraph,
            Indentation = first.Left > 72 ? first.Left - 72 : null,
            FontScale = first.Height > 0 && medianHeight > 0 ? Math.Round(first.Height / medianHeight, 2) : null,
            Runs = runs
        });

        lines.Clear();
    }

    private static ParliamentDocumentBlock ToBlock(
        PdfLine line,
        ParliamentDocumentBlockKind kind,
        int? headingLevel)
    {
        return new ParliamentDocumentBlock
        {
            Kind = kind,
            HeadingLevel = headingLevel,
            Indentation = line.Left > 72 ? line.Left - 72 : null,
            Runs =
            [
                new ParliamentDocumentRun
                {
                    Kind = ParliamentDocumentRunKind.Text,
                    Text = line.Text,
                    WidthEm = EstimateWidthEm(line.Text)
                }
            ]
        };
    }

    private static double EstimateWidthEm(string text)
    {
        return Math.Clamp(text.Length * 0.56, 1.4, 80);
    }

    private static double Median(List<double> values)
    {
        if (values.Count == 0)
        {
            return 10;
        }

        values.Sort();
        var middle = values.Count / 2;
        return values.Count % 2 == 0
            ? (values[middle - 1] + values[middle]) / 2
            : values[middle];
    }

    [GeneratedRegex(@"^(\d+[\.)]|[a-zA-Z][\.)]|[-•])\s+")]
    private static partial Regex ListPrefixRegex();

    [GeneratedRegex(@"^[^a-záàâãéèêíìóòôõúùç]{8,}$")]
    private static partial Regex UppercaseLetterRegex();

    private record PdfWord(
        string Text,
        double Left,
        double Bottom,
        double Right,
        double Top)
    {
        public double Baseline => Bottom;

        public double Height => Math.Max(1, Top - Bottom);
    }

    private record PdfLine(
        string Text,
        double Left,
        double Bottom,
        double Right,
        double Top,
        double Height);
}
