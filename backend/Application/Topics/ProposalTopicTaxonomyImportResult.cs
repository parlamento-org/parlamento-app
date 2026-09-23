namespace Parlamento.Application.Topics;

public record ProposalTopicTaxonomyImportResult(
    int ParentTopicsRead,
    int ParentTopicsInserted,
    int ParentTopicsUpdated,
    int SubtopicsRead,
    int SubtopicsInserted,
    int SubtopicsUpdated,
    int SeedAssignmentsRead,
    int SeedAssignmentsInserted,
    int SeedAssignmentsUpdated,
    int SeedAssignmentsSkipped,
    int SeedAssignmentsMissingProjectLaws,
    int SeedAssignmentsMissingSubtopics);
