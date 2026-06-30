using System.Text.Json.Serialization;

namespace Parlamento.Application.Votes;

public class ProposalFeedRequest
{
    [JsonPropertyName("userID")]
    public int UserId { get; set; }

    [JsonPropertyName("legislatura")]
    public List<string>? Legislatura { get; set; }

    [JsonPropertyName("legislaturas")]
    public List<string>? Legislaturas { get; set; }

    [JsonPropertyName("oldestVoteDate")]
    public string? OldestVoteDate { get; set; }

    [JsonPropertyName("newestVoteDate")]
    public string? NewestVoteDate { get; set; }

    [JsonPropertyName("lowestScoreAllowed")]
    public int LowestScoreAllowed { get; set; }

    public IReadOnlyList<string>? GetLegislaturas()
    {
        return Legislaturas ?? Legislatura;
    }
}
