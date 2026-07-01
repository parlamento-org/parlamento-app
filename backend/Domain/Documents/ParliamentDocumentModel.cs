namespace Parlamento.Domain.Documents;

public class ParliamentDocumentModel
{
    public string SchemaVersion { get; set; } = "parliament-document-v1";

    public string SourceKind { get; set; } = "Unknown";

    public List<ParliamentDocumentPage> Pages { get; set; } = [];
}

public class ParliamentDocumentPage
{
    public int PageNumber { get; set; }

    public double? Width { get; set; }

    public double? Height { get; set; }

    public List<ParliamentDocumentBlock> Blocks { get; set; } = [];
}

public class ParliamentDocumentBlock
{
    public ParliamentDocumentBlockKind Kind { get; set; }

    public int? HeadingLevel { get; set; }

    public string? Alignment { get; set; }

    public double? Indentation { get; set; }

    public double? FontScale { get; set; }

    public List<ParliamentDocumentRun> Runs { get; set; } = [];

    public ParliamentDocumentListKind? ListKind { get; set; }

    public List<ParliamentDocumentListItem> Items { get; set; } = [];

    public List<ParliamentDocumentTableRow> Rows { get; set; } = [];
}

public class ParliamentDocumentListItem
{
    public List<ParliamentDocumentBlock> Blocks { get; set; } = [];
}

public class ParliamentDocumentTableRow
{
    public List<ParliamentDocumentTableCell> Cells { get; set; } = [];
}

public class ParliamentDocumentTableCell
{
    public List<ParliamentDocumentBlock> Blocks { get; set; } = [];
}

public class ParliamentDocumentRun
{
    public ParliamentDocumentRunKind Kind { get; set; } = ParliamentDocumentRunKind.Text;

    public string? Text { get; set; }

    public bool Bold { get; set; }

    public bool Italic { get; set; }

    public bool Underline { get; set; }

    public string? Href { get; set; }

    public int? OriginalLength { get; set; }

    public double? WidthEm { get; set; }

    public string? RedactionKind { get; set; }
}

public enum ParliamentDocumentBlockKind
{
    Paragraph,
    Heading,
    List,
    Table,
    PageBreak
}

public enum ParliamentDocumentListKind
{
    Ordered,
    Unordered
}

public enum ParliamentDocumentRunKind
{
    Text,
    LineBreak,
    Redacted
}
