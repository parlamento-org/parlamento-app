namespace Parlamento.Application.Topics;

public class ProjectLawTopicAssignmentRequest
{
    public int? ProjectLawId { get; set; }

    public string? Legislature { get; set; }

    public bool Force { get; set; }

    public int? MaxDocuments { get; set; }
}
