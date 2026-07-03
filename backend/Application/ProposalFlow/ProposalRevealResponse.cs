using System.Text.Json.Serialization;

using Parlamento.Domain.Enums;

namespace Parlamento.Application.ProposalFlow;

public sealed class ProposalRevealResponse
{
    [JsonPropertyName("initiativeId")]
    public int InitiativeId { get; set; }

    [JsonPropertyName("initiativeType")]
    public string InitiativeType { get; set; } = string.Empty;

    [JsonPropertyName("initiativeNumber")]
    public string? InitiativeNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("userVote")]
    public ProposalInteractionType UserVote { get; set; }

    [JsonPropertyName("proposers")]
    public List<ProposalProposerResponse> Proposers { get; set; } = [];

    [JsonPropertyName("generalityVote")]
    public ParliamentaryVoteSummaryResponse? GeneralityVote { get; set; }

    [JsonPropertyName("officialSources")]
    public List<OfficialSourceLinkResponse> OfficialSources { get; set; } = [];

    [JsonPropertyName("journey")]
    public ProposalJourneyActionResponse Journey { get; set; } = new();
}
