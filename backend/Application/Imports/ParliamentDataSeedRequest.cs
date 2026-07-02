namespace Parlamento.Application.Imports;

public class ParliamentDataSeedRequest
{
    public List<string> Legislatures { get; } = [];

    public bool IncludeSummaries { get; set; } = true;

    public bool ForceRedaction { get; set; }

    public bool ForceSummaries { get; set; }

    public int? MaxDocuments { get; set; }
}
