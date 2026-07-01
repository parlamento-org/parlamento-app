using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Parlamento.Infrastructure.Services.ParliamentOpenData;

internal static partial class RedactionTextService
{
    public const string PolicyVersion = "party-and-deputy-names-v1";

    public static string RedactText(string text, IEnumerable<string> terms)
    {
        var redacted = text;

        foreach (var term in terms
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Select(x => x.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderByDescending(x => x.Length))
        {
            var pattern = $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(term)}(?![\p{{L}}\p{{N}}])";
            redacted = Regex.Replace(
                redacted,
                pattern,
                "[redigido]",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return redacted;
    }

    public static string PlainTextToHtml(string text)
    {
        var builder = new StringBuilder();
        builder.Append("<article class=\"proposal-text\">");

        foreach (var paragraph in ParagraphSplitRegex().Split(text).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            builder
                .Append("<p>")
                .Append(WebUtility.HtmlEncode(paragraph.Trim()))
                .Append("</p>");
        }

        builder.Append("</article>");
        return builder.ToString();
    }

    [GeneratedRegex(@"\r?\n\s*\r?\n+")]
    private static partial Regex ParagraphSplitRegex();
}
