using System.Text.Json.Serialization;

namespace Parlamento.Application.ProposalFlow;

public sealed class ProposalJourneyActionResponse
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = "Acompanhar percurso da proposta";

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;
}
