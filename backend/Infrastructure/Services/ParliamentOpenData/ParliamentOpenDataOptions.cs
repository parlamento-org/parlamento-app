namespace Parlamento.Infrastructure.Services.ParliamentOpenData;

public class ParliamentOpenDataOptions
{
    public const string SectionName = "ParliamentOpenData";

    public string? LatestLegislature { get; set; }

    public DailyParliamentImportOptions DailyImport { get; set; } = new();

    public Dictionary<string, ParliamentLegislatureSourceOptions> Legislatures { get; set; } = [];
}

public class DailyParliamentImportOptions
{
    public bool Enabled { get; set; }

    public string TimeZoneId { get; set; } = "Europe/Lisbon";

    public TimeSpan RunAt { get; set; } = TimeSpan.Zero;
}

public class ParliamentLegislatureSourceOptions
{
    public string? InitiativesUrl { get; set; }
}
