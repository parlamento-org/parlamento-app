using Parlamento.Application.Topics;

namespace Parlamento.Application.Abstractions;

public interface IProposalTopicTaxonomyImportService
{
    Task<ProposalTopicTaxonomyImportResult> ImportReviewedTaxonomyAsync(
        CancellationToken cancellationToken = default);
}
