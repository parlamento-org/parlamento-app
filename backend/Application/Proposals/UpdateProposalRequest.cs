using System.Text.Json.Serialization;

using Parlamento.Domain.Entities;
using Parlamento.Domain.Enums;

namespace Parlamento.Application.Proposals;

public class UpdateProposalRequest
{
    [JsonPropertyName("legislatura")]
    public string? Legislatura { get; set; }

    [JsonPropertyName("sourceId")]
    public int? SourceId { get; set; }

    [JsonPropertyName("score")]
    public int? Score { get; set; }

    [JsonPropertyName("voteDate")]
    public string? VoteDate { get; set; }

    [JsonPropertyName("proposingPartyAcronym")]
    public string? ProposingPartyAcronym { get; set; }

    [JsonPropertyName("proposalTitle")]
    public string? ProposalTitle { get; set; }

    [JsonPropertyName("fullProposalTextLink")]
    public string? FullProposalTextLink { get; set; }

    [JsonPropertyName("proposalTextHTML")]
    public string? ProposalTextHtml { get; set; }

    [JsonPropertyName("proposalResult")]
    public ProposalResult? ProposalResult { get; set; }

    [JsonPropertyName("votingResultGenerality")]
    public VotingResult? VotingResultGenerality { get; set; }

    [JsonPropertyName("votingResultSpeciality")]
    public VotingResult? VotingResultSpeciality { get; set; }
}
