using System.Text.Json.Serialization;

namespace Parlamento.Application.ProposalFlow;

public sealed class InitiativeFeedRequest
{
    [JsonPropertyName("legislatures")]
    public List<string>? Legislatures { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 1;
}
