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

    public string ExtractorVersion => "pdfpig-letter-document-extractor-v6-sequence-word-boundaries";

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
        var glyphs = page.Letters
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .Select(x => new PdfGlyph(
                x.Value,
                Convert.ToDouble(x.BoundingBox.Left),
                Convert.ToDouble(x.BoundingBox.Bottom),
                Convert.ToDouble(x.BoundingBox.Right),
                Convert.ToDouble(x.BoundingBox.Top),
                Convert.ToInt64(x.TextSequence)))
            .OrderByDescending(x => x.Top)
            .ThenBy(x => x.Left)
            .ToList();

        if (glyphs.Count == 0)
        {
            return [];
        }

        var lines = ReconstructLines(glyphs);
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

    public static List<string> ReconstructTextLinesForTests(IEnumerable<PdfGlyph> glyphs)
    {
        return ReconstructLines(glyphs.ToList()).Select(x => x.Text).ToList();
    }

    private static List<PdfLine> ReconstructLines(List<PdfGlyph> glyphs)
    {
        var lines = new List<List<PdfGlyph>>();
        foreach (var glyph in glyphs)
        {
            var line = lines.FirstOrDefault(existing =>
            {
                var averageHeight = existing.Average(x => x.Height);
                var tolerance = Math.Max(3.5, Math.Max(averageHeight, glyph.Height) * 0.6);
                return Math.Abs(existing.Average(x => x.Baseline) - glyph.Baseline) <= tolerance;
            });

            if (line is null)
            {
                lines.Add([glyph]);
            }
            else
            {
                line.Add(glyph);
            }
        }

        return lines
            .Select(lineGlyphs => lineGlyphs.OrderBy(x => x.Left).ToList())
            .Select(lineGlyphs => new PdfLine(
                ReconstructLineText(lineGlyphs),
                lineGlyphs.Min(x => x.Left),
                lineGlyphs.Min(x => x.Bottom),
                lineGlyphs.Max(x => x.Right),
                lineGlyphs.Max(x => x.Top),
                lineGlyphs.Average(x => x.Height)))
            .OrderByDescending(x => x.Top)
            .ThenBy(x => x.Left)
            .ToList();
    }

    private static string ReconstructLineText(IReadOnlyList<PdfGlyph> glyphs)
    {
        if (glyphs.Count == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        var ordered = glyphs.OrderBy(x => x.Left).ToList();
        var nonWhitespaceGlyphs = ordered.Where(x => !x.IsWhitespace).ToList();
        var adjacentGaps = nonWhitespaceGlyphs
            .Zip(nonWhitespaceGlyphs.Skip(1), (previous, current) => current.Left - previous.Right)
            .ToList();
        var sequenceGlyphCounts = nonWhitespaceGlyphs
            .GroupBy(x => x.TextSequence)
            .ToDictionary(x => x.Key, x => x.Count());
        var sequenceTexts = nonWhitespaceGlyphs
            .GroupBy(x => x.TextSequence)
            .ToDictionary(x => x.Key, x => string.Concat(x.OrderBy(glyph => glyph.Left).Select(glyph => glyph.Text)));
        var medianGlyphWidth = Median(nonWhitespaceGlyphs.Select(x => x.Width).Where(x => x > 0).ToList());
        var wordGapThreshold = DetermineWordGapThreshold(
            adjacentGaps,
            medianGlyphWidth);

        for (var i = 0; i < ordered.Count; i++)
        {
            var glyph = ordered[i];
            if (glyph.IsWhitespace)
            {
                AppendSpaceIfNeeded(builder);
                continue;
            }

            if (i > 0)
            {
                var previous = ordered.Take(i).LastOrDefault(x => !x.IsWhitespace);
                if (previous is not null &&
                    ShouldInsertSpace(
                        previous,
                        glyph,
                        glyph.Left - previous.Right,
                        wordGapThreshold,
                        medianGlyphWidth,
                        sequenceGlyphCounts,
                        sequenceTexts))
                {
                    AppendSpaceIfNeeded(builder);
                }
            }

            builder.Append(glyph.Text);
        }

        return NormalizeCharacterSpacedText(builder.ToString()).Trim();
    }

    private static bool ShouldInsertSpace(
        PdfGlyph previous,
        PdfGlyph current,
        double gap,
        double wordGapThreshold,
        double medianGlyphWidth,
        IReadOnlyDictionary<long, int> sequenceGlyphCounts,
        IReadOnlyDictionary<long, string> sequenceTexts)
    {
        if (gap > wordGapThreshold)
        {
            return true;
        }

        if (previous.TextSequence == current.TextSequence ||
            IsPunctuationWithoutFollowingSpace(previous.Text) ||
            !sequenceGlyphCounts.TryGetValue(previous.TextSequence, out var previousSequenceCount) ||
            !sequenceGlyphCounts.TryGetValue(current.TextSequence, out var currentSequenceCount) ||
            previousSequenceCount < 2 ||
            currentSequenceCount < 2)
        {
            return false;
        }

        var maximumTouchingGap = Math.Max(0.8, medianGlyphWidth * 0.2);
        if (gap < -maximumTouchingGap || gap > maximumTouchingGap)
        {
            return false;
        }

        var previousSequenceText = sequenceTexts.GetValueOrDefault(previous.TextSequence) ?? string.Empty;
        var currentSequenceText = sequenceTexts.GetValueOrDefault(current.TextSequence) ?? string.Empty;
        return IsLikelyWordBoundary(previousSequenceText, currentSequenceText);
    }

    private static bool IsPunctuationWithoutFollowingSpace(string text)
    {
        return text is "-" or "–" or "—" or "/" or "(" or "«";
    }

    private static bool IsLikelyWordBoundary(string previousSequenceText, string currentSequenceText)
    {
        if (string.IsNullOrWhiteSpace(previousSequenceText) || string.IsNullOrWhiteSpace(currentSequenceText))
        {
            return false;
        }

        if (char.IsUpper(currentSequenceText[0]))
        {
            return true;
        }

        if (IsCommonPortugueseConnector(previousSequenceText) || IsCommonPortugueseConnector(currentSequenceText))
        {
            return true;
        }

        return previousSequenceText.All(char.IsUpper) || currentSequenceText.All(char.IsUpper);
    }

    private static bool IsCommonPortugueseConnector(string text)
    {
        return text.ToLowerInvariant() is
            "a" or "as" or "ao" or "aos" or
            "da" or "das" or "de" or "do" or "dos" or
            "e" or "em" or
            "na" or "nas" or "no" or "nos" or
            "o" or "os" or
            "para" or "por" or
            "que" or
            "um" or "uma";
    }

    private static void AppendSpaceIfNeeded(System.Text.StringBuilder builder)
    {
        if (builder.Length > 0 && builder[^1] != ' ')
        {
            builder.Append(' ');
        }
    }

    private static string NormalizeCharacterSpacedText(string text)
    {
        var normalizedWords = CharacterSpacedWordRegex().Replace(
            text,
            match => match.Value.Replace(" ", string.Empty, StringComparison.Ordinal));

        return CharacterSpacedNumberRegex().Replace(
            normalizedWords,
            match => match.Value.Replace(" ", string.Empty, StringComparison.Ordinal));
    }

    private static double DetermineWordGapThreshold(List<double> adjacentGaps, double medianGlyphWidth)
    {
        var positiveGaps = adjacentGaps
            .Where(x => x > 0.05)
            .Order()
            .ToList();

        if (positiveGaps.Count == 0)
        {
            return double.PositiveInfinity;
        }

        var overlappingOrTouchingGaps = adjacentGaps.Count(x => x <= 0.05);
        if (overlappingOrTouchingGaps >= positiveGaps.Count * 2)
        {
            return positiveGaps[0] * 0.5;
        }

        var minimumMeaningfulDelta = Math.Max(0.2, medianGlyphWidth * 0.06);
        var bestScore = 0d;
        double? threshold = null;

        for (var i = 0; i < positiveGaps.Count - 1; i++)
        {
            var left = positiveGaps[i];
            var right = positiveGaps[i + 1];
            var delta = right - left;
            var ratio = right / Math.Max(left, 0.1);

            if (delta < minimumMeaningfulDelta || ratio < 1.7)
            {
                continue;
            }

            var score = ratio * delta;
            if (score > bestScore)
            {
                bestScore = score;
                threshold = (left + right) / 2;
            }
        }

        return threshold ?? positiveGaps[^1] + 0.1;
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

    private static bool HasPdfSignature(byte[] bytes)
    {
        return bytes.Length >= 4 &&
               bytes[0] == '%' &&
               bytes[1] == 'P' &&
               bytes[2] == 'D' &&
               bytes[3] == 'F';
    }

    [GeneratedRegex(@"^(\d+[\.)]|[a-zA-Z][\.)]|[-•])\s+")]
    private static partial Regex ListPrefixRegex();

    [GeneratedRegex(@"^[^a-záàâãéèêíìóòôõúùç]{8,}$")]
    private static partial Regex UppercaseLetterRegex();

    [GeneratedRegex(@"(?<!\p{L})(?:\p{L}\s+){2,}\p{L}(?!\p{L})")]
    private static partial Regex CharacterSpacedWordRegex();

    [GeneratedRegex(@"(?<!\p{N})(?:\p{N}\s+){1,}\p{N}(?!\p{N})")]
    private static partial Regex CharacterSpacedNumberRegex();

    public record PdfGlyph(
        string Text,
        double Left,
        double Bottom,
        double Right,
        double Top,
        long TextSequence = 0)
    {
        public double Baseline => Bottom;

        public double Height => Math.Max(1, Top - Bottom);

        public double Width => Math.Max(0.1, Right - Left);

        public bool IsWhitespace => string.IsNullOrWhiteSpace(Text);
    }

    private record PdfLine(
        string Text,
        double Left,
        double Bottom,
        double Right,
        double Top,
        double Height);
}
