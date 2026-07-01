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

    [JsonPropertyName("sourceIdText")]
    public string? SourceIdText { get; set; }

    [JsonPropertyName("initiativeNumber")]
    public string? InitiativeNumber { get; set; }

    [JsonPropertyName("initiativeTypeCode")]
    public string? InitiativeTypeCode { get; set; }

    [JsonPropertyName("initiativeTypeDescription")]
    public string? InitiativeTypeDescription { get; set; }

    [JsonPropertyName("initiativeSelection")]
    public string? InitiativeSelection { get; set; }

    [JsonPropertyName("initiativeObservations")]
    public string? InitiativeObservations { get; set; }

    [JsonPropertyName("initiativeTextSubstitution")]
    public string? InitiativeTextSubstitution { get; set; }

    [JsonPropertyName("initiativeTextSubstitutionField")]
    public string? InitiativeTextSubstitutionField { get; set; }

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
