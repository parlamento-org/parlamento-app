namespace Parlamento.Application.Summaries;

public record GeneratedParliamentSummary(
    string? ShortTitle,
    string SummaryText,
    IReadOnlyList<string> BulletPoints);
