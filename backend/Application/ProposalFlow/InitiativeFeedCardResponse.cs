using System.Text.Json.Serialization;

namespace Parlamento.Application.ProposalFlow;

public sealed class InitiativeFeedCardResponse
{
    [JsonPropertyName("initiativeId")]
    public int InitiativeId { get; set; }

    [JsonPropertyName("initiativeType")]
    public string InitiativeType { get; set; } = string.Empty;

    [JsonPropertyName("initiativeNumber")]
    public string? InitiativeNumber { get; set; }

    [JsonPropertyName("neutralTitle")]
    public string NeutralTitle { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("summaryGeneratedAtUtc")]
    public DateTime? SummaryGeneratedAtUtc { get; set; }

    [JsonPropertyName("redactedExcerpt")]
    public string? RedactedExcerpt { get; set; }

    [JsonPropertyName("redactedText")]
    public string? RedactedText { get; set; }

    [JsonPropertyName("legislature")]
    public string? Legislature { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }
}
