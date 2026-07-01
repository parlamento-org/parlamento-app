namespace Parlamento.Application.Summaries;

public class ParliamentSummaryRequest
{
    public int? ProjectLawId { get; set; }

    public string? Legislature { get; set; }

    public bool AllUnprocessed { get; set; }

    public bool Force { get; set; }

    public int? MaxDocuments { get; set; }
}
