namespace Parlamento.Application.Summaries;

public record GeneratedParliamentSummary(
    string ModelName,
    string? ShortTitle,
    string SummaryText,
    IReadOnlyList<string> BulletPoints);
