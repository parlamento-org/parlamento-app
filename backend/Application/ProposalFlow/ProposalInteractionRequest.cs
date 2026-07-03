using System.Text.Json.Serialization;

using Parlamento.Domain.Enums;

namespace Parlamento.Application.ProposalFlow;

public sealed class ProposalInteractionRequest
{
    [JsonPropertyName("initiativeId")]
    public int InitiativeId { get; set; }

    [JsonPropertyName("action")]
    public ProposalInteractionType Action { get; set; }

    [JsonPropertyName("idempotencyKey")]
    public string? IdempotencyKey { get; set; }
}
