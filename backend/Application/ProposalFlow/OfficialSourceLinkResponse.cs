using System.Text.Json.Serialization;

namespace Parlamento.Application.ProposalFlow;

public sealed class OfficialSourceLinkResponse
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
