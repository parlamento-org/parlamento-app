using Parlamento.Application.Abstractions;

internal static class ParliamentImportCommand
{
    public static async Task<bool> TryRunAsync(WebApplication app, string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "parliament-import", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var command = Parse(args.Skip(1).ToArray());

        using var scope = app.Services.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<IParliamentOpenDataImportService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (!string.IsNullOrWhiteSpace(command.FilePath))
        {
            app.Logger.LogInformation("Running parliament import command from local file {FilePath}.", command.FilePath);
            var fileResult = await importService.ImportFromFileAsync(command.FilePath);
            LogResult(app, fileResult);
            return true;
        }

        var legislatures = command.Legislatures;
        if (legislatures.Count == 0)
        {
            var latest = configuration["ParliamentOpenData:LatestLegislature"];
            if (!string.IsNullOrWhiteSpace(latest))
            {
                legislatures.Add(latest);
            }
        }

        if (legislatures.Count == 0)
        {
            throw new InvalidOperationException(
                "No legislatures were supplied and ParliamentOpenData:LatestLegislature is not configured.");
        }

        app.Logger.LogInformation(
            "Running parliament import command for legislatures: {Legislatures}.",
            string.Join(", ", legislatures));

        foreach (var legislature in legislatures)
        {
            var result = await importService.ImportLegislatureAsync(legislature);
            LogResult(app, result);
        }

        return true;
    }

    private static ParliamentImportCommandOptions Parse(string[] args)
    {
        var options = new ParliamentImportCommandOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--file", StringComparison.OrdinalIgnoreCase))
            {
                options.FilePath = RequireValue(args, ref i, "--file");
                continue;
            }

            if (string.Equals(arg, "--legislatures", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--legislature", StringComparison.OrdinalIgnoreCase))
            {
                while (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    i++;
                    AddLegislatures(options.Legislatures, args[i]);
                }

                continue;
            }

            if (string.Equals(arg, "--latest", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AddLegislatures(options.Legislatures, arg);
        }

        return options;
    }

    private static string RequireValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"{optionName} requires a value.");
        }

        index++;
        return args[index];
    }

    private static void AddLegislatures(List<string> legislatures, string value)
    {
        foreach (var legislature in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!legislatures.Contains(legislature, StringComparer.OrdinalIgnoreCase))
            {
                legislatures.Add(legislature);
            }
        }
    }

    private static void LogResult(WebApplication app, Parlamento.Application.Imports.ParliamentImportRunResult result)
    {
        app.Logger.LogInformation(
            "Parliament import command result. RunId={RunId} Status={Status} Read={Read} Inserted={Inserted} Updated={Updated} Skipped={Skipped} Failed={Failed}",
            result.RunId,
            result.Status,
            result.RecordsRead,
            result.RecordsInserted,
            result.RecordsUpdated,
            result.RecordsSkipped,
            result.RecordsFailed);
    }

    private sealed class ParliamentImportCommandOptions
    {
        public string? FilePath { get; set; }

        public List<string> Legislatures { get; } = [];
    }
}
