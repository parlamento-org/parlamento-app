using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

using Parlamento.Domain.Enums;

namespace Parlamento.Infrastructure.Services.ParliamentOpenData;

internal static partial class VoteDetailParser
{
    public static IReadOnlyList<ParsedVoteBlock> Parse(
        string? detail,
        string? unanimous,
        string? absencesJson = null,
        bool includeUnanimousBlock = true)
    {
        var blocks = new List<ParsedVoteBlock>();
        var isUnanimous = IsTruthyUnanimous(unanimous);

        if (isUnanimous && includeUnanimousBlock)
        {
            blocks.Add(new ParsedVoteBlock(
                VotingOrientation.InFavor,
                null,
                null,
                true,
                unanimous!.Trim(),
                null));
        }

        if (!string.IsNullOrWhiteSpace(detail))
        {
            var normalized = WebUtility.HtmlDecode(detail);
            normalized = BrRegex().Replace(normalized, "\n");
            normalized = ItalicRegex().Replace(normalized, string.Empty);

            foreach (var section in SplitSections(normalized))
            {
                foreach (var token in section.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    blocks.Add(ParseToken(section.Key, token));
                }
            }
        }

        if (isUnanimous && !blocks.Any(x => x.Orientation == VotingOrientation.Absent))
        {
            foreach (var absence in ParseAbsenceTokens(absencesJson))
            {
                blocks.Add(ParseToken(VotingOrientation.Absent, absence));
            }
        }

        MarkSplitParties(blocks);
        return blocks;
    }

    private static Dictionary<VotingOrientation, string> SplitSections(string text)
    {
        var matches = SectionRegex().Matches(text).ToList();
        var sections = new Dictionary<VotingOrientation, string>();

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var valueStart = match.Index + match.Length;
            var valueEnd = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var label = NormalizeLabel(match.Groups["label"].Value);
            var orientation = LabelToOrientation(label);

            if (orientation is null)
            {
                continue;
            }

            sections[orientation.Value] = text[valueStart..valueEnd].Trim();
        }

        return sections;
    }

    private static ParsedVoteBlock ParseToken(VotingOrientation orientation, string token)
    {
        var cleaned = token.Trim();

        if (orientation == VotingOrientation.Absent)
        {
            var absenceBlock = ParseAbsenceToken(cleaned);
            if (absenceBlock is not null)
            {
                return absenceBlock;
            }
        }

        var match = PartyTokenRegex().Match(cleaned);

        if (!match.Success)
        {
            return new ParsedVoteBlock(
                orientation,
                null,
                null,
                null,
                cleaned,
                "Could not parse vote token.");
        }

        var countGroup = match.Groups["count"];
        var count = countGroup.Success ? int.Parse(countGroup.Value) : null as int?;
        var party = match.Groups["party"].Value.Trim();

        return new ParsedVoteBlock(
            orientation,
            party,
            count,
            !count.HasValue,
            cleaned,
            null);
    }

    private static ParsedVoteBlock? ParseAbsenceToken(string cleaned)
    {
        var deputyMatch = DeputyAbsenceTokenRegex().Match(cleaned);
        if (deputyMatch.Success)
        {
            return new ParsedVoteBlock(
                VotingOrientation.Absent,
                deputyMatch.Groups["party"].Value.Trim(),
                null,
                null,
                cleaned,
                null);
        }

        var partyMatch = AbsencePartyTokenRegex().Match(cleaned);
        if (!partyMatch.Success)
        {
            return null;
        }

        return new ParsedVoteBlock(
            VotingOrientation.Absent,
            partyMatch.Groups["party"].Value.Trim(),
            null,
            true,
            cleaned,
            null);
    }

    private static IEnumerable<string> ParseAbsenceTokens(string? absencesJson)
    {
        if (string.IsNullOrWhiteSpace(absencesJson))
        {
            yield break;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(absencesJson);
        }
        catch (JsonException)
        {
            yield break;
        }

        using (document)
        {
            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                var value = document.RootElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    yield return value;
                }

                yield break;
            }

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            foreach (var item in document.RootElement.EnumerateArray())
            {
                var value = item.ValueKind == JsonValueKind.String
                    ? item.GetString()
                    : item.GetRawText();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    yield return value;
                }
            }
        }
    }

    private static string NormalizeLabel(string value)
    {
        return WebUtility.HtmlDecode(value)
            .Trim()
            .ToLowerInvariant()
            .Replace("ç", "c")
            .Replace("ã", "a")
            .Replace("ê", "e")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("Ã§", "c")
            .Replace("Ã£", "a")
            .Replace("Ãª", "e")
            .Replace("Ã©", "e")
            .Replace("Ã­", "i");
    }

    private static VotingOrientation? LabelToOrientation(string label)
    {
        return label switch
        {
            "a favor" => VotingOrientation.InFavor,
            "contra" => VotingOrientation.Against,
            "abstencao" => VotingOrientation.Abstaining,
            "ausencia" => VotingOrientation.Absent,
            "ausencias" => VotingOrientation.Absent,
            _ => null
        };
    }

    private static void MarkSplitParties(List<ParsedVoteBlock> blocks)
    {
        var splitParties = blocks
            .Where(x => !string.IsNullOrWhiteSpace(x.PartyAcronym))
            .GroupBy(x => x.PartyAcronym!)
            .Where(x => x.Select(y => y.Orientation).Distinct().Count() > 1)
            .Select(x => x.Key)
            .ToHashSet();

        for (var i = 0; i < blocks.Count; i++)
        {
            var partyAcronym = blocks[i].PartyAcronym;
            if (partyAcronym is null || !splitParties.Contains(partyAcronym))
            {
                continue;
            }

            blocks[i] = blocks[i] with { IsUnanimousWithinParty = false };
        }
    }

    private static bool IsTruthyUnanimous(string? unanimous)
    {
        return !string.IsNullOrWhiteSpace(unanimous);
    }

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrRegex();

    [GeneratedRegex(@"</?i>", RegexOptions.IgnoreCase)]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"(?<label>A Favor|Contra|Absten(?:ç|Ã§|c)(?:ã|Ã£|a)o|Aus(?:ê|Ãª|e)ncia|Aus(?:ê|Ãª|e)ncias)\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex SectionRegex();

    [GeneratedRegex(@"^(?:(?<count>\d+)\s*-\s*)?(?<party>[A-Z][A-Z0-9-]{0,12})$")]
    private static partial Regex PartyTokenRegex();

    [GeneratedRegex(@"^.+\((?<party>[A-Za-z][A-Za-z0-9-]{0,12})\)$")]
    private static partial Regex DeputyAbsenceTokenRegex();

    [GeneratedRegex(@"^(?<party>[A-Za-z][A-Za-z0-9-]{0,12})$")]
    private static partial Regex AbsencePartyTokenRegex();
}
