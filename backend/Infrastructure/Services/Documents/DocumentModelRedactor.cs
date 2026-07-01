using System.Text;
using System.Text.RegularExpressions;

using Parlamento.Application.Abstractions;
using Parlamento.Domain.Documents;

namespace Parlamento.Infrastructure.Services.Documents;

public partial class DocumentModelRedactor : IDocumentModelRedactor
{
    public string PolicyVersion => "party-and-deputy-names-structured-v5-case-sensitive-acronyms";

    public ParliamentDocumentModel Redact(
        ParliamentDocumentModel document,
        IEnumerable<string> terms)
    {
        var normalizedTerms = terms
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(x => x.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Length)
            .ToList();

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
        if (terms.Count == 0 || runs.All(x => x.Kind != ParliamentDocumentRunKind.Text))
        {
            return runs.ToList();
        }

        var flatText = new StringBuilder();
        var map = new List<RunCharacterMap>();

        for (var runIndex = 0; runIndex < runs.Count; runIndex++)
        {
            var run = runs[runIndex];
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

        var matches = FindMatches(flatText.ToString(), terms);
        if (matches.Count == 0)
        {
            return runs.ToList();
        }

        var runMatches = matches
            .Select(match => new RunMatch(
                map[match.Start].RunIndex,
                map[match.Start].CharIndex,
                map[match.Start + match.Length - 1].RunIndex,
                map[match.Start + match.Length - 1].CharIndex,
                match.Length))
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

    private static List<TextMatch> FindMatches(string text, IReadOnlyList<string> terms)
    {
        var matches = new List<TextMatch>();
        var occupied = new bool[text.Length];

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
                    occupied.Skip(match.Index).Take(match.Length).Any(x => x))
                {
                    continue;
                }

                for (var i = match.Index; i < match.Index + match.Length; i++)
                {
                    occupied[i] = true;
                }

                matches.Add(new TextMatch(match.Index, match.Length));
            }
        }

        return matches.OrderBy(x => x.Start).ToList();
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

    private record TextMatch(int Start, int Length);

    private record RunMatch(
        int StartRunIndex,
        int StartCharIndex,
        int EndRunIndex,
        int EndCharIndex,
        int Length);
}
