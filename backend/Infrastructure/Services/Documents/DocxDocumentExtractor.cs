using System.IO.Compression;
using System.Xml.Linq;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Documents;
using Parlamento.Domain.Documents;

namespace Parlamento.Infrastructure.Services.Documents;

public class DocxDocumentExtractor : IDocumentExtractor
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/package/2006/relationships";

    public string ExtractorKind => "Docx";

    public string ExtractorVersion => "docx-document-extractor-v1";

    public bool CanExtract(string sourceName)
    {
        return sourceName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public bool CanExtract(byte[] bytes, string sourceName)
    {
        return CanExtract(sourceName) && HasZipSignature(bytes);
    }

    public Task<DocumentExtractionResult> ExtractAsync(
        byte[] bytes,
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        if (!HasZipSignature(bytes))
        {
            throw new InvalidDataException(
                $"Document '{sourceName}' is named like a DOCX, but the downloaded bytes are not an Open XML ZIP package.");
        }

        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var documentEntry = archive.GetEntry("word/document.xml")
                            ?? throw new InvalidOperationException("DOCX file does not contain word/document.xml.");

        var hyperlinks = ReadHyperlinkRelationships(archive);
        using var documentStream = documentEntry.Open();
        var xDocument = XDocument.Load(documentStream);
        var body = xDocument.Root?.Element(W + "body")
                   ?? throw new InvalidOperationException("DOCX file does not contain a document body.");

        var page = new ParliamentDocumentPage { PageNumber = 1 };
        foreach (var element in body.Elements())
        {
            if (element.Name == W + "p")
            {
                var block = BuildParagraph(element, hyperlinks);
                if (block.Runs.Count > 0)
                {
                    page.Blocks.Add(block);
                }
            }
            else if (element.Name == W + "tbl")
            {
                var table = BuildTable(element, hyperlinks);
                if (table.Rows.Count > 0)
                {
                    page.Blocks.Add(table);
                }
            }
        }

        var document = new ParliamentDocumentModel
        {
            SourceKind = "Docx",
            Pages = [page]
        };

        return Task.FromResult(new DocumentExtractionResult(
            document,
            ExtractorKind,
            ExtractorVersion));
    }

    private static Dictionary<string, string> ReadHyperlinkRelationships(ZipArchive archive)
    {
        var entry = archive.GetEntry("word/_rels/document.xml.rels");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var relationships = XDocument.Load(stream);
        return relationships
            .Descendants(Rel + "Relationship")
            .Where(x => string.Equals((string?)x.Attribute("Type"), "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink", StringComparison.Ordinal))
            .Where(x => x.Attribute("Id") is not null && x.Attribute("Target") is not null)
            .ToDictionary(
                x => (string)x.Attribute("Id")!,
                x => (string)x.Attribute("Target")!,
                StringComparer.OrdinalIgnoreCase);
    }

    private static ParliamentDocumentBlock BuildParagraph(
        XElement paragraph,
        IReadOnlyDictionary<string, string> hyperlinks)
    {
        var style = (string?)paragraph
            .Element(W + "pPr")
            ?.Element(W + "pStyle")
            ?.Attribute(W + "val");
        var isList = paragraph.Element(W + "pPr")?.Element(W + "numPr") is not null;
        var headingLevel = TryGetHeadingLevel(style);
        var runs = new List<ParliamentDocumentRun>();

        foreach (var child in paragraph.Elements())
        {
            if (child.Name == W + "r")
            {
                AppendRuns(child, runs, null);
            }
            else if (child.Name == W + "hyperlink")
            {
                var relationId = (string?)child.Attribute(R + "id");
                var href = relationId is not null && hyperlinks.TryGetValue(relationId, out var target)
                    ? target
                    : null;

                foreach (var hyperlinkRun in child.Elements(W + "r"))
                {
                    AppendRuns(hyperlinkRun, runs, href);
                }
            }
        }

        return new ParliamentDocumentBlock
        {
            Kind = headingLevel.HasValue
                ? ParliamentDocumentBlockKind.Heading
                : ParliamentDocumentBlockKind.Paragraph,
            HeadingLevel = headingLevel,
            Indentation = TryGetIndentation(paragraph),
            Runs = isList ? PrefixListMarker(runs) : runs
        };
    }

    private static ParliamentDocumentBlock BuildTable(
        XElement table,
        IReadOnlyDictionary<string, string> hyperlinks)
    {
        var block = new ParliamentDocumentBlock { Kind = ParliamentDocumentBlockKind.Table };
        foreach (var row in table.Elements(W + "tr"))
        {
            var tableRow = new ParliamentDocumentTableRow();
            foreach (var cell in row.Elements(W + "tc"))
            {
                var tableCell = new ParliamentDocumentTableCell();
                foreach (var paragraph in cell.Elements(W + "p"))
                {
                    var cellParagraph = BuildParagraph(paragraph, hyperlinks);
                    if (cellParagraph.Runs.Count > 0)
                    {
                        tableCell.Blocks.Add(cellParagraph);
                    }
                }

                tableRow.Cells.Add(tableCell);
            }

            block.Rows.Add(tableRow);
        }

        return block;
    }

    private static void AppendRuns(
        XElement run,
        List<ParliamentDocumentRun> runs,
        string? href)
    {
        var runProperties = run.Element(W + "rPr");
        var bold = runProperties?.Element(W + "b") is not null;
        var italic = runProperties?.Element(W + "i") is not null;
        var underline = runProperties?.Element(W + "u") is not null;

        foreach (var element in run.Elements())
        {
            if (element.Name == W + "br")
            {
                runs.Add(new ParliamentDocumentRun { Kind = ParliamentDocumentRunKind.LineBreak });
                continue;
            }

            if (element.Name != W + "t")
            {
                continue;
            }

            var text = element.Value;
            if (string.IsNullOrEmpty(text))
            {
                continue;
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
    }

    private static List<ParliamentDocumentRun> PrefixListMarker(List<ParliamentDocumentRun> runs)
    {
        return
        [
            new ParliamentDocumentRun { Kind = ParliamentDocumentRunKind.Text, Text = "- " },
            ..runs
        ];
    }

    private static int? TryGetHeadingLevel(string? style)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return null;
        }

        if (style.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(style["Heading".Length..], out var headingLevel))
        {
            return Math.Clamp(headingLevel, 1, 6);
        }

        if (style.StartsWith("Título", StringComparison.OrdinalIgnoreCase) ||
            style.StartsWith("Title", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return null;
    }

    private static double? TryGetIndentation(XElement paragraph)
    {
        var left = (string?)paragraph
            .Element(W + "pPr")
            ?.Element(W + "ind")
            ?.Attribute(W + "left");

        return double.TryParse(left, out var twips)
            ? twips / 20
            : null;
    }

    private static bool HasZipSignature(byte[] bytes)
    {
        return bytes.Length >= 4 &&
               bytes[0] == 'P' &&
               bytes[1] == 'K' &&
               (bytes[2] == 3 || bytes[2] == 5 || bytes[2] == 7) &&
               (bytes[3] == 4 || bytes[3] == 6 || bytes[3] == 8);
    }
}
