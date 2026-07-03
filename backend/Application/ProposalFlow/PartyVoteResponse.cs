using System.Text.Json.Serialization;

using Parlamento.Domain.Enums;

namespace Parlamento.Application.ProposalFlow;

public sealed class PartyVoteResponse
{
    [JsonPropertyName("partyAcronym")]
    public string PartyAcronym { get; set; } = string.Empty;

    [JsonPropertyName("orientation")]
    public VotingOrientation Orientation { get; set; }

    [JsonPropertyName("numberOfDeputies")]
    public int? NumberOfDeputies { get; set; }

    [JsonPropertyName("isUnanimousWithinParty")]
    public bool? IsUnanimousWithinParty { get; set; }
}
