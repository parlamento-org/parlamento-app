namespace Parlamento.Application.Topics;

public record ProjectLawTopicAssignmentRunResult(
    int DocumentsRead,
    int AssignmentsCreated,
    int AssignmentsSkipped,
    int AssignmentsFailed,
    int AutoAssigned,
    int NeedsReview,
    int Unassigned,
    int MissingRedactedText,
    int PreservedReviewedAssignments);
