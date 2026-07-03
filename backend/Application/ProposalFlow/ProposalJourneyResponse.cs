using System.Text.Json.Serialization;

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

    [JsonPropertyName("phases")]
    public List<ProposalJourneyPhaseResponse> Phases { get; set; } = [];
}
