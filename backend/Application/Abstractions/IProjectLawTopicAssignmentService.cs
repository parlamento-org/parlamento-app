using Parlamento.Application.Topics;

namespace Parlamento.Application.Abstractions;

public interface IProjectLawTopicAssignmentService
{
    Task<ProjectLawTopicAssignmentRunResult> AssignMissingAsync(
        ProjectLawTopicAssignmentRequest request,
        CancellationToken cancellationToken = default);
}
