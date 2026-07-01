namespace Parlamento.Infrastructure.Services.Summaries;

public class OpenAiSummaryOptions
{
    public const string SectionName = "OpenAI";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4o-mini-2024-07-18";

    public double Temperature { get; set; } = 0.1;

    public int MaxOutputTokens { get; set; } = 700;
}
