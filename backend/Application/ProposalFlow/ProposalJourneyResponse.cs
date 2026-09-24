using System.Text.Json.Serialization;

using Parlamento.Domain.Enums;

namespace Parlamento.Application.ProposalFlow;

public sealed class ProposalJourneyResponse
{
    [JsonPropertyName("initiativeId")]
    public int InitiativeId { get; set; }

    [JsonPropertyName("initiativeType")]
    public string InitiativeType { get; set; } = string.Empty;

    [JsonPropertyName("initiativeNumber")]
    public string? InitiativeNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("fullProposalTextLink")]
    public string? FullProposalTextLink { get; set; }

    [JsonPropertyName("userVote")]
    public ProposalInteractionType? UserVote { get; set; }

    [JsonPropertyName("generalityVote")]
    public ParliamentaryVoteSummaryResponse? GeneralityVote { get; set; }

    [JsonPropertyName("proposers")]
    public List<ProposalProposerResponse> Proposers { get; set; } = [];

    [JsonPropertyName("topicAssignments")]
    public List<ProposalTopicAssignmentResponse> TopicAssignments { get; set; } = [];

    [JsonPropertyName("phases")]
    public List<ProposalJourneyPhaseResponse> Phases { get; set; } = [];
}
