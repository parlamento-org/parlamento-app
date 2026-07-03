using System.Text.Json.Serialization;

namespace Parlamento.Application.ProposalFlow;

public sealed class ProposalJourneyPhaseResponse
{
    [JsonPropertyName("phaseCode")]
    public string? PhaseCode { get; set; }

    [JsonPropertyName("phaseName")]
    public string PhaseName { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("observation")]
    public string? Observation { get; set; }

    [JsonPropertyName("approvedTextId")]
    public string? ApprovedTextId { get; set; }

    [JsonPropertyName("votes")]
    public List<ParliamentaryVoteSummaryResponse> Votes { get; set; } = [];

    [JsonPropertyName("documents")]
    public List<OfficialSourceLinkResponse> Documents { get; set; } = [];

    [JsonPropertyName("diaryLinks")]
    public List<OfficialSourceLinkResponse> DiaryLinks { get; set; } = [];

    [JsonPropertyName("videos")]
    public List<ProposalJourneyVideoResponse> Videos { get; set; } = [];

    [JsonPropertyName("transcripts")]
    public List<OfficialSourceLinkResponse> Transcripts { get; set; } = [];
}
