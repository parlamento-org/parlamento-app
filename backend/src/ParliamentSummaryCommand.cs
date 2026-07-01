using Parlamento.Application.Abstractions;
using Parlamento.Application.Summaries;

internal static class ParliamentSummaryCommand
{
    public static async Task<bool> TryRunAsync(WebApplication app, string[] args)
    {
        if (args.Length == 0 ||
            !string.Equals(args[0], "parliament-summary", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var command = Parse(args.Skip(1).ToArray());
        if (!command.ProjectLawId.HasValue &&
            string.IsNullOrWhiteSpace(command.Legislature) &&
            !command.AllUnprocessed &&
            !command.Force)
        {
            command.AllUnprocessed = true;
        }

        using var scope = app.Services.CreateScope();
        var summaryService = scope.ServiceProvider.GetRequiredService<IParliamentSummaryService>();

        app.Logger.LogInformation(
            "Running parliament summary command. ProjectLawId={ProjectLawId} Legislature={Legislature} AllUnprocessed={AllUnprocessed} Force={Force} MaxDocuments={MaxDocuments}",
            command.ProjectLawId,
            command.Legislature,
            command.AllUnprocessed,
            command.Force,
            command.MaxDocuments);

        var result = await summaryService.GenerateSummariesAsync(new ParliamentSummaryRequest
        {
            ProjectLawId = command.ProjectLawId,
            Legislature = command.Legislature,
            AllUnprocessed = command.AllUnprocessed,
            Force = command.Force,
            MaxDocuments = command.MaxDocuments
        });

        app.Logger.LogInformation(
            "Parliament summary command result. Read={Read} Generated={Generated} Skipped={Skipped} Failed={Failed}",
            result.DocumentsRead,
            result.SummariesGenerated,
            result.SummariesSkipped,
            result.SummariesFailed);

        return true;
    }

    private static ParliamentSummaryCommandOptions Parse(string[] args)
    {
        var options = new ParliamentSummaryCommandOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--initiative-id", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--project-law-id", StringComparison.OrdinalIgnoreCase))
            {
                options.ProjectLawId = int.Parse(RequireValue(args, ref i, arg));
                continue;
            }

            if (string.Equals(arg, "--legislature", StringComparison.OrdinalIgnoreCase))
            {
                options.Legislature = RequireValue(args, ref i, "--legislature");
                continue;
            }

            if (string.Equals(arg, "--all-unprocessed", StringComparison.OrdinalIgnoreCase))
            {
                options.AllUnprocessed = true;
                continue;
            }

            if (string.Equals(arg, "--force", StringComparison.OrdinalIgnoreCase))
            {
                options.Force = true;
                continue;
            }

            if (string.Equals(arg, "--max-documents", StringComparison.OrdinalIgnoreCase))
            {
                options.MaxDocuments = Math.Max(1, int.Parse(RequireValue(args, ref i, "--max-documents")));
                continue;
            }

            if (int.TryParse(arg, out var projectLawId))
            {
                options.ProjectLawId = projectLawId;
            }
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

    private sealed class ParliamentSummaryCommandOptions
    {
        public int? ProjectLawId { get; set; }

        public string? Legislature { get; set; }

        public bool AllUnprocessed { get; set; }

        public bool Force { get; set; }

        public int? MaxDocuments { get; set; }
    }
}
