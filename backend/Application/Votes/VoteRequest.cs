using System.Text.Json.Serialization;

using Parlamento.Domain.Enums;

namespace Parlamento.Application.Votes;

public class VoteRequest
{
    [JsonPropertyName("userID")]
    public int UserId { get; set; }

    [JsonPropertyName("projectLawID")]
    public int ProjectLawId { get; set; }

    [JsonPropertyName("votingOrientation")]
    public VotingOrientation VotingOrientation { get; set; }
}
