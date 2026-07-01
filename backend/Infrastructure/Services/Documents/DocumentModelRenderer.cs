using System.Globalization;
using System.Net;
using System.Text;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Documents;
using Parlamento.Domain.Documents;

namespace Parlamento.Infrastructure.Services.Documents;

public class DocumentModelRenderer : IDocumentModelRenderer
{
    public const string RendererVersion = "document-model-html-v1";

    string IDocumentModelRenderer.RendererVersion => RendererVersion;

    public DocumentRenderResult Render(ParliamentDocumentModel document)
    {
        return new DocumentRenderResult(
            RenderHtml(document),
            RenderPlainText(document),
            RendererVersion);
    }

    private static string RenderHtml(ParliamentDocumentModel document)
    {
        var builder = new StringBuilder();
        builder.Append("<article class=\"proposal-document\" data-schema=\"")
            .Append(WebUtility.HtmlEncode(document.SchemaVersion))
            .Append("\">");

        foreach (var page in document.Pages.OrderBy(x => x.PageNumber))
        {
            builder.Append("<section class=\"proposal-page\" data-page=\"")
                .Append(page.PageNumber.ToString(CultureInfo.InvariantCulture))
                .Append("\"");

            if (page.Width.HasValue)
            {
                builder.Append(" data-width=\"")
                    .Append(page.Width.Value.ToString("0.##", CultureInfo.InvariantCulture))
                    .Append("\"");
            }

            if (page.Height.HasValue)
            {
                builder.Append(" data-height=\"")
                    .Append(page.Height.Value.ToString("0.##", CultureInfo.InvariantCulture))
                    .Append("\"");
            }

            builder.Append(">");
            RenderBlocksHtml(builder, page.Blocks);
            builder.Append("</section>");
        }

        builder.Append("</article>");
        return builder.ToString();
    }

    private static void RenderBlocksHtml(
        StringBuilder builder,
        IReadOnlyList<ParliamentDocumentBlock> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block.Kind)
            {
                case ParliamentDocumentBlockKind.Heading:
                    var level = Math.Clamp(block.HeadingLevel ?? 2, 1, 6);
                    builder.Append("<h").Append(level);
                    AppendBlockStyle(builder, block);
                    builder.Append(">");
                    RenderRunsHtml(builder, block.Runs);
                    builder.Append("</h").Append(level).Append(">");
                    break;
                case ParliamentDocumentBlockKind.List:
                    builder.Append(block.ListKind == ParliamentDocumentListKind.Ordered ? "<ol>" : "<ul>");
                    foreach (var item in block.Items)
                    {
                        builder.Append("<li>");
                        RenderBlocksHtml(builder, item.Blocks);
                        builder.Append("</li>");
                    }

                    builder.Append(block.ListKind == ParliamentDocumentListKind.Ordered ? "</ol>" : "</ul>");
                    break;
                case ParliamentDocumentBlockKind.Table:
                    builder.Append("<table>");
                    foreach (var row in block.Rows)
                    {
                        builder.Append("<tr>");
                        foreach (var cell in row.Cells)
                        {
                            builder.Append("<td>");
                            RenderBlocksHtml(builder, cell.Blocks);
                            builder.Append("</td>");
                        }

                        builder.Append("</tr>");
                    }

                    builder.Append("</table>");
                    break;
                case ParliamentDocumentBlockKind.PageBreak:
                    builder.Append("<hr class=\"page-break\" />");
                    break;
                default:
                    builder.Append("<p");
                    AppendBlockStyle(builder, block);
                    builder.Append(">");
                    RenderRunsHtml(builder, block.Runs);
                    builder.Append("</p>");
                    break;
            }
        }
    }

    private static void AppendBlockStyle(StringBuilder builder, ParliamentDocumentBlock block)
    {
        var styles = new List<string>();

        if (!string.IsNullOrWhiteSpace(block.Alignment))
        {
            styles.Add($"text-align:{CssEncode(block.Alignment)}");
        }

        if (block.Indentation is > 0)
        {
            styles.Add($"margin-left:{block.Indentation.Value.ToString("0.##", CultureInfo.InvariantCulture)}pt");
        }

        if (block.FontScale is > 0 and not 1)
        {
            styles.Add($"font-size:{block.FontScale.Value.ToString("0.##", CultureInfo.InvariantCulture)}em");
        }

        if (styles.Count > 0)
        {
            builder.Append(" style=\"")
                .Append(string.Join(';', styles))
                .Append("\"");
        }
    }

    private static void RenderRunsHtml(
        StringBuilder builder,
        IReadOnlyList<ParliamentDocumentRun> runs)
    {
        foreach (var run in runs)
        {
            if (run.Kind == ParliamentDocumentRunKind.LineBreak)
            {
                builder.Append("<br />");
                continue;
            }

            if (run.Kind == ParliamentDocumentRunKind.Redacted)
            {
                var width = (run.WidthEm ?? Math.Max(1.4, (run.OriginalLength ?? 8) * 0.56))
                    .ToString("0.##", CultureInfo.InvariantCulture);
                builder.Append("<span class=\"redacted\" data-length=\"")
                    .Append((run.OriginalLength ?? 0).ToString(CultureInfo.InvariantCulture))
                    .Append("\" style=\"--redaction-width:")
                    .Append(width)
                    .Append("em\"></span>");
                continue;
            }

            RenderTextRunHtml(builder, run);
        }
    }

    private static void RenderTextRunHtml(StringBuilder builder, ParliamentDocumentRun run)
    {
        var closeTags = new Stack<string>();

        if (!string.IsNullOrWhiteSpace(run.Href))
        {
            builder.Append("<a href=\"")
                .Append(WebUtility.HtmlEncode(run.Href))
                .Append("\">");
            closeTags.Push("</a>");
        }

        if (run.Bold)
        {
            builder.Append("<strong>");
            closeTags.Push("</strong>");
        }

        if (run.Italic)
        {
            builder.Append("<em>");
            closeTags.Push("</em>");
        }

        if (run.Underline)
        {
            builder.Append("<u>");
            closeTags.Push("</u>");
        }

        builder.Append(WebUtility.HtmlEncode(run.Text ?? string.Empty));

        while (closeTags.Count > 0)
        {
            builder.Append(closeTags.Pop());
        }
    }

    private static string RenderPlainText(ParliamentDocumentModel document)
    {
        var builder = new StringBuilder();
        foreach (var page in document.Pages.OrderBy(x => x.PageNumber))
        {
            RenderBlocksText(builder, page.Blocks, 0);
        }

        return builder.ToString().Trim();
    }

    private static void RenderBlocksText(
        StringBuilder builder,
        IReadOnlyList<ParliamentDocumentBlock> blocks,
        int indent)
    {
        foreach (var block in blocks)
        {
            switch (block.Kind)
            {
                case ParliamentDocumentBlockKind.List:
                    for (var i = 0; i < block.Items.Count; i++)
                    {
                        builder.Append(' ', indent)
                            .Append(block.ListKind == ParliamentDocumentListKind.Ordered ? $"{i + 1}. " : "- ");
                        RenderBlocksText(builder, block.Items[i].Blocks, indent + 2);
                    }

                    builder.AppendLine();
                    break;
                case ParliamentDocumentBlockKind.Table:
                    foreach (var row in block.Rows)
                    {
                        builder.AppendLine(string.Join(
                            "\t",
                            row.Cells.Select(cell => RenderCellText(cell))));
                    }

                    builder.AppendLine();
                    break;
                case ParliamentDocumentBlockKind.PageBreak:
                    builder.AppendLine();
                    break;
                default:
                    builder.Append(' ', indent);
                    RenderRunsText(builder, block.Runs);
                    builder.AppendLine();
                    builder.AppendLine();
                    break;
            }
        }
    }

    private static string RenderCellText(ParliamentDocumentTableCell cell)
    {
        var builder = new StringBuilder();
        RenderBlocksText(builder, cell.Blocks, 0);
        return builder.ToString().ReplaceLineEndings(" ").Trim();
    }

    private static void RenderRunsText(
        StringBuilder builder,
        IReadOnlyList<ParliamentDocumentRun> runs)
    {
        foreach (var run in runs)
        {
            builder.Append(run.Kind switch
            {
                ParliamentDocumentRunKind.LineBreak => Environment.NewLine,
                ParliamentDocumentRunKind.Redacted => "[redigido]",
                _ => run.Text
            });
        }
    }

    private static string CssEncode(string value)
    {
        return value switch
        {
            "left" or "right" or "center" or "justify" => value,
            _ => "left"
        };
    }
}
