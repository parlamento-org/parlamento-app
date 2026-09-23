using System.Text.Json.Serialization;

namespace Parlamento.Application.ProposalFlow;

public sealed class ProposalTopicAssignmentResponse
{
    [JsonPropertyName("parentTopicSlug")]
    public string ParentTopicSlug { get; set; } = string.Empty;

    [JsonPropertyName("parentTopicLabel")]
    public string ParentTopicLabel { get; set; } = string.Empty;

    [JsonPropertyName("subtopicSlug")]
    public string SubtopicSlug { get; set; } = string.Empty;

    [JsonPropertyName("subtopicLabel")]
    public string SubtopicLabel { get; set; } = string.Empty;

    [JsonPropertyName("assignmentStatus")]
    public string AssignmentStatus { get; set; } = string.Empty;

    [JsonPropertyName("assignmentConfidence")]
    public double? AssignmentConfidence { get; set; }
}