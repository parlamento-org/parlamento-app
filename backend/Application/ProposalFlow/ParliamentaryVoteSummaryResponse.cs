using System.Text.Json.Serialization;

namespace Parlamento.Application.ProposalFlow;

public sealed class ParliamentaryVoteSummaryResponse
{
    [JsonPropertyName("stageCode")]
    public string StageCode { get; set; } = string.Empty;

    [JsonPropertyName("stageName")]
    public string StageName { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("approved")]
    public bool? Approved { get; set; }

    [JsonPropertyName("isUnanimous")]
    public bool IsUnanimous { get; set; }

    [JsonPropertyName("partyVotes")]
    public List<PartyVoteResponse> PartyVotes { get; set; } = [];
}
