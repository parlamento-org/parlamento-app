using Parlamento.Application.Summaries;

namespace Parlamento.Application.Abstractions;

public interface ILegislativeSummaryClient
{
    string ModelName { get; }

    IReadOnlyList<string> ModelNames { get; }

    string PromptVersion { get; }

    Task<GeneratedParliamentSummary> GenerateSummaryAsync(
        string redactedPlainText,
        CancellationToken cancellationToken = default);
}
