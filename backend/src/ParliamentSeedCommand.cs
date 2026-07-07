using Parlamento.Application.Abstractions;
using Parlamento.Application.Imports;

internal static class ParliamentSeedCommand
{
    public static async Task<bool> TryRunAsync(WebApplication app, string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "parliament-seed", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var command = Parse(args.Skip(1).ToArray());

        using var scope = app.Services.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<IParliamentDataSeedService>();

        app.Logger.LogInformation(
            "Running parliament seed command. Legislatures={Legislatures} IncludeSummaries={IncludeSummaries} MaxDocuments={MaxDocuments} ForceImport={ForceImport} ForceRedaction={ForceRedaction} ForceSummaries={ForceSummaries}",
            command.Legislatures.Count == 0 ? "<all configured>" : string.Join(", ", command.Legislatures),
            command.IncludeSummaries,
            command.MaxDocuments,
            command.ForceImport,
            command.ForceRedaction,
            command.ForceSummaries);

        var result = await seedService.SeedAsync(command);
        app.Logger.LogInformation(
            "Parliament seed command result. Legislatures={Legislatures} Succeeded={Succeeded} Failed={Failed} ImportRead={ImportRead} ImportInserted={ImportInserted} ImportUpdated={ImportUpdated} ImportSkipped={ImportSkipped} ImportFailed={ImportFailed} DocumentsRead={DocumentsRead} DocumentsProcessed={DocumentsProcessed} DocumentsSkipped={DocumentsSkipped} DocumentsFailed={DocumentsFailed} SummaryDocumentsRead={SummaryDocumentsRead} SummariesGenerated={SummariesGenerated} SummariesSkipped={SummariesSkipped} SummariesFailed={SummariesFailed}",
            string.Join(", ", result.Legislatures),
            result.LegislaturesSucceeded,
            result.LegislaturesFailed,
            result.ImportRecordsRead,
            result.ImportRecordsInserted,
            result.ImportRecordsUpdated,
            result.ImportRecordsSkipped,
            result.ImportRecordsFailed,
            result.DocumentsRead,
            result.DocumentsProcessed,
            result.DocumentsSkipped,
            result.DocumentsFailed,
            result.SummaryDocumentsRead,
            result.SummariesGenerated,
            result.SummariesSkipped,
            result.SummariesFailed);

        Console.WriteLine("Parliament seed completed.");
        Console.WriteLine($"Legislatures: {string.Join(", ", result.Legislatures)}");
        Console.WriteLine($"Succeeded: {result.LegislaturesSucceeded}; failed: {result.LegislaturesFailed}");
        Console.WriteLine($"Import: read={result.ImportRecordsRead}, inserted={result.ImportRecordsInserted}, updated={result.ImportRecordsUpdated}, skipped={result.ImportRecordsSkipped}, failed={result.ImportRecordsFailed}");
        Console.WriteLine($"Documents: read={result.DocumentsRead}, processed={result.DocumentsProcessed}, skipped={result.DocumentsSkipped}, failed={result.DocumentsFailed}");
        if (result.SummariesSkippedBecauseOpenAiIsNotConfigured)
        {
            Console.WriteLine("Summaries: skipped because OPENAI_API_KEY/OpenAI:ApiKey is not configured.");
        }
        else if (result.SummariesRequested)
        {
            Console.WriteLine($"Summaries: read={result.SummaryDocumentsRead}, generated={result.SummariesGenerated}, skipped={result.SummariesSkipped}, failed={result.SummariesFailed}");
        }
        else
        {
            Console.WriteLine("Summaries: disabled by command flag.");
        }

        return true;
    }

    private static ParliamentDataSeedRequest Parse(string[] args)
    {
        var options = new ParliamentDataSeedRequest();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
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

            if (string.Equals(arg, "--no-summary", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--skip-summary", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--skip-summaries", StringComparison.OrdinalIgnoreCase))
            {
                options.IncludeSummaries = false;
                continue;
            }

            if (string.Equals(arg, "--force-redaction", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--force-upsert", StringComparison.OrdinalIgnoreCase))
            {
                options.ForceRedaction = true;
                continue;
            }

            if (string.Equals(arg, "--force-import", StringComparison.OrdinalIgnoreCase))
            {
                options.ForceImport = true;
                continue;
            }

            if (string.Equals(arg, "--force-summary", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--force-summaries", StringComparison.OrdinalIgnoreCase))
            {
                options.ForceSummaries = true;
                continue;
            }

            if (string.Equals(arg, "--force", StringComparison.OrdinalIgnoreCase))
            {
                options.ForceImport = true;
                options.ForceRedaction = true;
                options.ForceSummaries = true;
                continue;
            }

            if (string.Equals(arg, "--max-documents", StringComparison.OrdinalIgnoreCase))
            {
                options.MaxDocuments = Math.Max(1, int.Parse(RequireValue(args, ref i, "--max-documents")));
                continue;
            }

            if (string.Equals(arg, "--all", StringComparison.OrdinalIgnoreCase))
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
}
