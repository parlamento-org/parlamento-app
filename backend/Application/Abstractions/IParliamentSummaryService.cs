using Parlamento.Application.Summaries;

namespace Parlamento.Application.Abstractions;

public interface IParliamentSummaryService
{
    Task<ParliamentSummaryRunResult> GenerateSummariesAsync(
        ParliamentSummaryRequest request,
        CancellationToken cancellationToken = default);
}
