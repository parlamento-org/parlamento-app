using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Parlamento.Application.Abstractions;
using Parlamento.Domain.Documents;

namespace Parlamento.Infrastructure.Services.Documents;

public partial class DocumentModelRedactor : IDocumentModelRedactor
{
    private static readonly Regex EmailRegex = new(
        @"(?<![\w.%+-])[\w.%+-]+@[\w.-]+\.[A-Za-z]{2,}(?![\w.-])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UrlRegex = new(
        @"(?<![\w@])(?:https?://|www\.)[^\s<>'"")\]]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex DomainRegex = new(
        @"(?<![\w@])(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+(?:pt|com|org|net|eu)(?:/[^\s<>'"")\]]*)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public string PolicyVersion => "party-deputy-email-link-redaction-v8";

    public ParliamentDocumentModel Redact(
        ParliamentDocumentModel document,
        IEnumerable<string> terms)
    {
        var normalizedTerms = NormalizeTerms(terms);

        foreach (var page in document.Pages)
        {
            RedactBlocks(page.Blocks, normalizedTerms);
        }

        return document;
    }

    private static void RedactBlocks(
        List<ParliamentDocumentBlock> blocks,
        IReadOnlyList<string> terms)
    {
        RedactTermsAcrossBlocks(blocks, terms);

        foreach (var block in blocks)
        {
            if (block.Runs.Count > 0)
            {
                block.Runs = RedactRuns(block.Runs, terms);
            }

            foreach (var item in block.Items)
            {
                RedactBlocks(item.Blocks, terms);
            }

            foreach (var row in block.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    RedactBlocks(cell.Blocks, terms);
                }
            }
        }
    }

    private static List<ParliamentDocumentRun> RedactRuns(
        IReadOnlyList<ParliamentDocumentRun> runs,
        IReadOnlyList<string> terms)
    {
        runs = NormalizeTextRuns(runs);

        if (runs.All(x => x.Kind != ParliamentDocumentRunKind.Text))
        {
            return runs.ToList();
        }

        var flatText = new StringBuilder();
        var map = new List<RunCharacterMap>();

        for (var runIndex = 0; runIndex < runs.Count; runIndex++)
        {
            var run = runs[runIndex];
            if (run.Kind == ParliamentDocumentRunKind.LineBreak)
            {
                flatText.Append('\n');
                map.Add(new RunCharacterMap(runIndex, -1));
                continue;
            }

            if (run.Kind != ParliamentDocumentRunKind.Text || string.IsNullOrEmpty(run.Text))
            {
                continue;
            }

            for (var charIndex = 0; charIndex < run.Text.Length; charIndex++)
            {
                flatText.Append(run.Text[charIndex]);
                map.Add(new RunCharacterMap(runIndex, charIndex));
            }
        }

        var matches = FindMatches(flatText.ToString(), terms, runs, map);
        if (matches.Count == 0)
        {
            return runs.ToList();
        }

        var runMatches = matches
            .Select(match => ToRunMatch(match, map))
            .Where(match => match != null)
            .Select(match => match!)
            .OrderBy(x => x.StartRunIndex)
            .ThenBy(x => x.StartCharIndex)
            .ToList();

        var output = new List<ParliamentDocumentRun>();
        for (var runIndex = 0; runIndex < runs.Count; runIndex++)
        {
            var run = runs[runIndex];
            if (run.Kind != ParliamentDocumentRunKind.Text || string.IsNullOrEmpty(run.Text))
            {
                output.Add(run);
                continue;
            }

            var matchesCoveringRun = runMatches
                .Where(x => x.StartRunIndex <= runIndex && x.EndRunIndex >= runIndex)
                .ToList();

            if (matchesCoveringRun.Count == 0)
            {
                output.Add(run);
                continue;
            }

            var cursor = 0;
            foreach (var match in matchesCoveringRun)
            {
                var startInRun = match.StartRunIndex == runIndex ? match.StartCharIndex : 0;
                var endInRun = match.EndRunIndex == runIndex ? match.EndCharIndex : run.Text.Length - 1;

                if (startInRun > cursor)
                {
                    output.Add(CloneTextSlice(run, cursor, startInRun - cursor));
                }

                if (match.StartRunIndex == runIndex)
                {
                    output.Add(new ParliamentDocumentRun
                    {
                        Kind = ParliamentDocumentRunKind.Redacted,
                        OriginalLength = match.Length,
                        WidthEm = EstimateWidthEm(match.Length),
                        RedactionKind = "term"
                    });
                }

                cursor = Math.Max(cursor, endInRun + 1);
            }

            if (cursor < run.Text.Length)
            {
                output.Add(CloneTextSlice(run, cursor, run.Text.Length - cursor));
            }
        }

        return output;
    }

    private static void RedactTermsAcrossBlocks(
        List<ParliamentDocumentBlock> blocks,
        IReadOnlyList<string> terms)
    {
        if (blocks.Count < 2 || terms.Count == 0)
        {
            return;
        }

        for (var blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
        {
            if (blocks[blockIndex].Runs.Count > 0)
            {
                blocks[blockIndex].Runs = NormalizeTextRuns(blocks[blockIndex].Runs).ToList();
            }
        }

        var flatText = new StringBuilder();
        var map = new List<BlockCharacterMap>();
        for (var blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
        {
            var block = blocks[blockIndex];
            if (block.Runs.Count == 0)
            {
                continue;
            }

            if (flatText.Length > 0)
            {
                flatText.Append('\n');
                map.Add(new BlockCharacterMap(-1, -1, -1));
            }

            AppendBlockRuns(flatText, map, block.Runs, blockIndex);
        }

        if (flatText.Length == 0)
        {
            return;
        }

        var text = flatText.ToString();
        var occupied = new bool[text.Length];
        var matches = new List<TextMatch>();
        foreach (var term in terms)
        {
            var pattern = BuildMatchPattern(term);
            var regexOptions = RegexOptions.CultureInvariant;
            if (!IsAcronymTerm(term))
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            foreach (Match match in Regex.Matches(text, pattern, regexOptions))
            {
                if (match.Length == 0 ||
                    IsOccupied(occupied, match.Index, match.Length) ||
                    !MatchSpansMultipleBlocks(match.Index, match.Length, map))
                {
                    continue;
                }

                AddMatch(matches, occupied, match.Index, match.Length);
            }
        }

        var blockMatches = matches
            .Select(match => ToBlockRunMatch(match, map))
            .Where(match => match != null)
            .Select(match => match!)
            .OrderBy(match => match.StartBlockIndex)
            .ThenBy(match => match.StartRunIndex)
            .ThenBy(match => match.StartCharIndex)
            .ToList();

        if (blockMatches.Count == 0)
        {
            return;
        }

        ApplyBlockRunMatches(blocks, blockMatches);
    }

    private static void AppendBlockRuns(
        StringBuilder flatText,
        List<BlockCharacterMap> map,
        IReadOnlyList<ParliamentDocumentRun> runs,
        int blockIndex)
    {
        for (var runIndex = 0; runIndex < runs.Count; runIndex++)
        {
            var run = runs[runIndex];
            if (run.Kind == ParliamentDocumentRunKind.LineBreak)
            {
                flatText.Append('\n');
                map.Add(new BlockCharacterMap(blockIndex, runIndex, -1));
                continue;
            }

            if (run.Kind != ParliamentDocumentRunKind.Text || string.IsNullOrEmpty(run.Text))
            {
                continue;
            }

            for (var charIndex = 0; charIndex < run.Text.Length; charIndex++)
            {
                flatText.Append(run.Text[charIndex]);
                map.Add(new BlockCharacterMap(blockIndex, runIndex, charIndex));
            }
        }
    }

    private static bool MatchSpansMultipleBlocks(
        int start,
        int length,
        IReadOnlyList<BlockCharacterMap> map)
    {
        var first = FirstTextMap(map, start, length);
        var last = LastTextMap(map, start, length);
        return first != null &&
               last != null &&
               first.BlockIndex != last.BlockIndex;
    }

    private static void ApplyBlockRunMatches(
        List<ParliamentDocumentBlock> blocks,
        IReadOnlyList<BlockRunMatch> matches)
    {
        for (var blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
        {
            var block = blocks[blockIndex];
            if (block.Runs.Count == 0)
            {
                continue;
            }

            var output = new List<ParliamentDocumentRun>();
            for (var runIndex = 0; runIndex < block.Runs.Count; runIndex++)
            {
                var run = block.Runs[runIndex];
                if (run.Kind != ParliamentDocumentRunKind.Text || string.IsNullOrEmpty(run.Text))
                {
                    output.Add(run);
                    continue;
                }

                var matchesCoveringRun = matches
                    .Where(match => CoversRun(match, blockIndex, runIndex))
                    .ToList();
                if (matchesCoveringRun.Count == 0)
                {
                    output.Add(run);
                    continue;
                }

                var cursor = 0;
                foreach (var match in matchesCoveringRun)
                {
                    var startInRun = match.StartBlockIndex == blockIndex && match.StartRunIndex == runIndex
                        ? match.StartCharIndex
                        : 0;
                    var endInRun = match.EndBlockIndex == blockIndex && match.EndRunIndex == runIndex
                        ? match.EndCharIndex
                        : run.Text.Length - 1;

                    if (startInRun > cursor)
                    {
                        output.Add(CloneTextSlice(run, cursor, startInRun - cursor));
                    }

                    if (match.StartBlockIndex == blockIndex && match.StartRunIndex == runIndex)
                    {
                        output.Add(new ParliamentDocumentRun
                        {
                            Kind = ParliamentDocumentRunKind.Redacted,
                            OriginalLength = match.Length,
                            WidthEm = EstimateWidthEm(match.Length),
                            RedactionKind = "term"
                        });
                    }

                    cursor = Math.Max(cursor, endInRun + 1);
                }

                if (cursor < run.Text.Length)
                {
                    output.Add(CloneTextSlice(run, cursor, run.Text.Length - cursor));
                }
            }

            block.Runs = output;
        }
    }

    private static List<string> NormalizeTerms(IEnumerable<string> terms)
    {
        return terms
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(CreateTermVariants)
            .Where(x => x.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Length)
            .ToList();
    }

    private static IEnumerable<string> CreateTermVariants(string term)
    {
        var normalized = NormalizeUnicode(term.Trim());
        yield return normalized;

        var withoutDiacritics = RemoveDiacritics(normalized);
        if (!string.Equals(normalized, withoutDiacritics, StringComparison.Ordinal))
        {
            yield return withoutDiacritics;
        }
    }

    private static IReadOnlyList<ParliamentDocumentRun> NormalizeTextRuns(
        IReadOnlyList<ParliamentDocumentRun> runs)
    {
        return runs
            .Select(run =>
            {
                if (run.Kind != ParliamentDocumentRunKind.Text || string.IsNullOrEmpty(run.Text))
                {
                    return run;
                }

                var normalizedText = NormalizeUnicode(run.Text);
                return string.Equals(run.Text, normalizedText, StringComparison.Ordinal)
                    ? run
                    : CloneTextRun(run, normalizedText);
            })
            .ToList();
    }

    private static List<TextMatch> FindMatches(
        string text,
        IReadOnlyList<string> terms,
        IReadOnlyList<ParliamentDocumentRun> runs,
        IReadOnlyList<RunCharacterMap> map)
    {
        var matches = new List<TextMatch>();
        var occupied = new bool[text.Length];

        AddSensitiveMatches(text, matches, occupied);
        AddHyperlinkMatches(runs, map, matches, occupied);

        foreach (var term in terms)
        {
            var pattern = BuildMatchPattern(term);
            var regexOptions = RegexOptions.CultureInvariant;
            if (!IsAcronymTerm(term))
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            foreach (Match match in Regex.Matches(
                         text,
                         pattern,
                         regexOptions))
            {
                if (match.Length == 0 ||
                    IsOccupied(occupied, match.Index, match.Length))
                {
                    continue;
                }

                AddMatch(matches, occupied, match.Index, match.Length);
            }
        }

        return matches.OrderBy(x => x.Start).ToList();
    }

    private static void AddSensitiveMatches(
        string text,
        List<TextMatch> matches,
        bool[] occupied)
    {
        foreach (Match match in EmailRegex.Matches(text))
        {
            AddMatchIfFree(matches, occupied, match.Index, TrimTrailingPunctuation(match.Value).Length);
        }

        foreach (Match match in UrlRegex.Matches(text))
        {
            AddMatchIfFree(matches, occupied, match.Index, TrimTrailingPunctuation(match.Value).Length);
        }

        foreach (Match match in DomainRegex.Matches(text))
        {
            AddMatchIfFree(matches, occupied, match.Index, TrimTrailingPunctuation(match.Value).Length);
        }
    }

    private static void AddHyperlinkMatches(
        IReadOnlyList<ParliamentDocumentRun> runs,
        IReadOnlyList<RunCharacterMap> map,
        List<TextMatch> matches,
        bool[] occupied)
    {
        var flatIndexByRunCharacter = map
            .Select((entry, flatIndex) => new { entry.RunIndex, entry.CharIndex, FlatIndex = flatIndex })
            .ToDictionary(x => (x.RunIndex, x.CharIndex), x => x.FlatIndex);

        for (var runIndex = 0; runIndex < runs.Count; runIndex++)
        {
            var run = runs[runIndex];
            if (run.Kind != ParliamentDocumentRunKind.Text ||
                string.IsNullOrEmpty(run.Text) ||
                string.IsNullOrWhiteSpace(run.Href))
            {
                continue;
            }

            if (flatIndexByRunCharacter.TryGetValue((runIndex, 0), out var start))
            {
                AddMatchIfFree(matches, occupied, start, run.Text.Length);
            }
        }
    }

    private static void AddMatchIfFree(
        List<TextMatch> matches,
        bool[] occupied,
        int start,
        int length)
    {
        if (length <= 0 || start < 0 || start + length > occupied.Length)
        {
            return;
        }

        if (IsOccupied(occupied, start, length))
        {
            return;
        }

        AddMatch(matches, occupied, start, length);
    }

    private static void AddMatch(
        List<TextMatch> matches,
        bool[] occupied,
        int start,
        int length)
    {
        for (var i = start; i < start + length; i++)
        {
            occupied[i] = true;
        }

        matches.Add(new TextMatch(start, length));
    }

    private static bool IsOccupied(bool[] occupied, int start, int length)
    {
        return occupied.Skip(start).Take(length).Any(x => x);
    }

    private static RunMatch? ToRunMatch(
        TextMatch match,
        IReadOnlyList<RunCharacterMap> map)
    {
        var first = FirstTextMap(map, match.Start, match.Length);
        var last = LastTextMap(map, match.Start, match.Length);
        return first == null || last == null
            ? null
            : new RunMatch(
                first.RunIndex,
                first.CharIndex,
                last.RunIndex,
                last.CharIndex,
                match.Length);
    }

    private static BlockRunMatch? ToBlockRunMatch(
        TextMatch match,
        IReadOnlyList<BlockCharacterMap> map)
    {
        var first = FirstTextMap(map, match.Start, match.Length);
        var last = LastTextMap(map, match.Start, match.Length);
        return first == null || last == null
            ? null
            : new BlockRunMatch(
                first.BlockIndex,
                first.RunIndex,
                first.CharIndex,
                last.BlockIndex,
                last.RunIndex,
                last.CharIndex,
                match.Length);
    }

    private static RunCharacterMap? FirstTextMap(
        IReadOnlyList<RunCharacterMap> map,
        int start,
        int length)
    {
        return map
            .Skip(start)
            .Take(length)
            .FirstOrDefault(entry => entry.CharIndex >= 0);
    }

    private static RunCharacterMap? LastTextMap(
        IReadOnlyList<RunCharacterMap> map,
        int start,
        int length)
    {
        return map
            .Skip(start)
            .Take(length)
            .LastOrDefault(entry => entry.CharIndex >= 0);
    }

    private static BlockCharacterMap? FirstTextMap(
        IReadOnlyList<BlockCharacterMap> map,
        int start,
        int length)
    {
        return map
            .Skip(start)
            .Take(length)
            .FirstOrDefault(entry => entry.BlockIndex >= 0 && entry.CharIndex >= 0);
    }

    private static BlockCharacterMap? LastTextMap(
        IReadOnlyList<BlockCharacterMap> map,
        int start,
        int length)
    {
        return map
            .Skip(start)
            .Take(length)
            .LastOrDefault(entry => entry.BlockIndex >= 0 && entry.CharIndex >= 0);
    }

    private static bool CoversRun(
        BlockRunMatch match,
        int blockIndex,
        int runIndex)
    {
        if (blockIndex < match.StartBlockIndex || blockIndex > match.EndBlockIndex)
        {
            return false;
        }

        if (blockIndex == match.StartBlockIndex && runIndex < match.StartRunIndex)
        {
            return false;
        }

        if (blockIndex == match.EndBlockIndex && runIndex > match.EndRunIndex)
        {
            return false;
        }

        return true;
    }

    private static string TrimTrailingPunctuation(string value)
    {
        return value.TrimEnd('.', ',', ';', ':', '!', '?');
    }

    private static string BuildMatchPattern(string term)
    {
        var termPattern = BuildTermPattern(term);
        var boundedPattern = $@"(?<![\p{{L}}\p{{N}}]){termPattern}(?![\p{{L}}\p{{N}}])";

        if (!CanMatchInsideGluedText(term))
        {
            return boundedPattern;
        }

        var gluedPattern = $@"(?<=[\p{{L}}\p{{N}}]){termPattern}(?![\p{{Ll}}\p{{N}}])";
        return $@"(?:{boundedPattern}|{gluedPattern})";
    }

    private static bool IsAcronymTerm(string term)
    {
        return term.Length >= 2 &&
               term.All(character => char.IsUpper(character) || char.IsDigit(character) || character == '-');
    }

    private static bool CanMatchInsideGluedText(string term)
    {
        return term.Length >= 4 &&
               !term.Any(char.IsWhiteSpace) &&
               term.All(character => char.IsLetter(character) || character == '-');
    }

    private static string BuildTermPattern(string term)
    {
        if (term.Length < 3)
        {
            return Regex.Escape(term);
        }

        var builder = new StringBuilder();
        for (var i = 0; i < term.Length; i++)
        {
            var character = term[i];
            builder.Append(char.IsWhiteSpace(character) ? @"\s+" : Regex.Escape(character.ToString()));

            if (i < term.Length - 1 && !char.IsWhiteSpace(character) && !char.IsWhiteSpace(term[i + 1]))
            {
                builder.Append(@"\s*");
            }
        }

        return builder.ToString();
    }

    private static string NormalizeUnicode(string value)
    {
        return value.Normalize(NormalizationForm.FormC);
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static ParliamentDocumentRun CloneTextRun(
        ParliamentDocumentRun source,
        string text)
    {
        return new ParliamentDocumentRun
        {
            Kind = ParliamentDocumentRunKind.Text,
            Text = text,
            Bold = source.Bold,
            Italic = source.Italic,
            Underline = source.Underline,
            Href = source.Href,
            WidthEm = source.WidthEm
        };
    }

    private static ParliamentDocumentRun CloneTextSlice(
        ParliamentDocumentRun source,
        int start,
        int length)
    {
        return new ParliamentDocumentRun
        {
            Kind = ParliamentDocumentRunKind.Text,
            Text = source.Text!.Substring(start, length),
            Bold = source.Bold,
            Italic = source.Italic,
            Underline = source.Underline,
            Href = source.Href,
            WidthEm = source.WidthEm.HasValue
                ? source.WidthEm.Value * length / source.Text.Length
                : null
        };
    }

    private static double EstimateWidthEm(int length)
    {
        return Math.Clamp(length * 0.56, 1.4, 24);
    }

    private record RunCharacterMap(int RunIndex, int CharIndex);

    private record BlockCharacterMap(
        int BlockIndex,
        int RunIndex,
        int CharIndex);

    private record TextMatch(int Start, int Length);

    private record RunMatch(
        int StartRunIndex,
        int StartCharIndex,
        int EndRunIndex,
        int EndCharIndex,
        int Length);

    private record BlockRunMatch(
        int StartBlockIndex,
        int StartRunIndex,
        int StartCharIndex,
        int EndBlockIndex,
        int EndRunIndex,
        int EndCharIndex,
        int Length);
}
