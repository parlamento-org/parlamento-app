using Parlamento.Application.Summaries;

namespace Parlamento.Application.Abstractions;

public interface ILegislativeSummaryClient
{
    string ModelName { get; }

    string PromptVersion { get; }

    Task<GeneratedParliamentSummary> GenerateSummaryAsync(
        string redactedPlainText,
        CancellationToken cancellationToken = default);
}
