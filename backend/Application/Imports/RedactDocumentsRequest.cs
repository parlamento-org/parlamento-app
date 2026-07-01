namespace Parlamento.Application.Imports;

public class RedactDocumentsRequest
{
    public string? Legislature { get; set; }

    public int? ProjectLawId { get; set; }

    public int MaxDocuments { get; set; } = 10;

    public bool ForceUpsert { get; set; }
}
