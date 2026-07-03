using System.Text.Json.Serialization;

namespace Parlamento.Application.ProposalFlow;

public sealed class ProposalJourneyVideoResponse
{
    [JsonPropertyName("speakerName")]
    public string? SpeakerName { get; set; }

    [JsonPropertyName("speakerParty")]
    public string? SpeakerParty { get; set; }

    [JsonPropertyName("governmentMemberName")]
    public string? GovernmentMemberName { get; set; }

    [JsonPropertyName("governmentMemberRole")]
    public string? GovernmentMemberRole { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("startTime")]
    public string? StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public string? EndTime { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
