using System.Text.Json.Serialization;

namespace Parlamento.Application.ProposalFlow;

public sealed class ProposalJourneyActionResponse
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = "Follow the proposal's journey";

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;
}
