using Parlamento.Application.Abstractions;
using Parlamento.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

internal static class ParliamentDocumentCommand
{
    public static async Task<bool> TryRunAsync(WebApplication app, string[] args)
    {
        if (args.Length == 0 ||
            !string.Equals(args[0], "parliament-documents", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (args.Length < 2 || !string.Equals(args[1], "redact", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Expected command: parliament-documents redact");
        }

        var command = Parse(args.Skip(2).ToArray());

        using var scope = app.Services.CreateScope();
        var redactionService = scope.ServiceProvider.GetRequiredService<IParliamentDocumentRedactionService>();
        var baseInfoImportService = scope.ServiceProvider.GetRequiredService<IParliamentBaseInfoImportService>();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (command.ProjectLawId.HasValue)
        {
            var legislature = await context.ProjectLaws
                .Where(x => x.Id == command.ProjectLawId.Value)
                .Select(x => x.Legislatura)
                .SingleOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(legislature))
            {
                throw new InvalidOperationException($"ProjectLawId={command.ProjectLawId.Value} was not found.");
            }

            await EnsureBaseInfoAsync(app, context, baseInfoImportService, legislature);

            app.Logger.LogInformation(
                "Running parliament document redaction command for ProjectLawId={ProjectLawId}. ForceUpsert={ForceUpsert}.",
                command.ProjectLawId.Value,
                command.ForceUpsert);

            var result = await redactionService.ProcessInitiativeTextDocumentsAsync(
                null,
                command.ProjectLawId.Value,
                command.MaxDocuments,
                command.ForceUpsert);
            LogResult(app, result);
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

        foreach (var legislature in legislatures)
        {
            await EnsureBaseInfoAsync(app, context, baseInfoImportService, legislature);

            app.Logger.LogInformation(
                "Running parliament document redaction command for Legislature={Legislature} MaxDocuments={MaxDocuments} ForceUpsert={ForceUpsert}.",
                legislature,
                command.MaxDocuments,
                command.ForceUpsert);

            var result = await redactionService.ProcessInitiativeTextDocumentsAsync(
                legislature,
                null,
                command.MaxDocuments,
                command.ForceUpsert);
            LogResult(app, result);
        }

        return true;
    }

    private static async Task EnsureBaseInfoAsync(
        WebApplication app,
        DatabaseContext context,
        IParliamentBaseInfoImportService baseInfoImportService,
        string legislature)
    {
        var hasDeputies = await context.ParliamentDeputies.AnyAsync(x => x.Legislature == legislature);
        var hasGroups = await context.ParliamentaryGroups.AnyAsync(x => x.Legislature == legislature);
        var hasTerms = await context.ParliamentRedactionTerms.AnyAsync(x => x.Legislature == legislature);

        if (hasDeputies && hasGroups && hasTerms)
        {
            return;
        }

        app.Logger.LogInformation(
            "Base information is incomplete for {Legislature}. DeputiesLoaded={DeputiesLoaded} GroupsLoaded={GroupsLoaded} TermsLoaded={TermsLoaded}. Importing base-info before document redaction.",
            legislature,
            hasDeputies,
            hasGroups,
            hasTerms);

        var result = await baseInfoImportService.ImportLegislatureAsync(legislature);
        app.Logger.LogInformation(
            "Base-info preflight completed for {Legislature}. Deputies={Deputies} ParliamentaryGroups={Groups} RedactionTerms={Terms}",
            result.Legislature,
            result.DeputiesRead,
            result.ParliamentaryGroupsRead,
            result.RedactionTermsRebuilt);
    }

    private static ParliamentDocumentCommandOptions Parse(string[] args)
    {
        var options = new ParliamentDocumentCommandOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--project-law-id", StringComparison.OrdinalIgnoreCase))
            {
                options.ProjectLawId = int.Parse(RequireValue(args, ref i, "--project-law-id"));
                continue;
            }

            if (string.Equals(arg, "--max-documents", StringComparison.OrdinalIgnoreCase))
            {
                options.MaxDocuments = Math.Max(1, int.Parse(RequireValue(args, ref i, "--max-documents")));
                continue;
            }

            if (string.Equals(arg, "--force-upsert", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--force", StringComparison.OrdinalIgnoreCase))
            {
                options.ForceUpsert = true;
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

    private static void LogResult(
        WebApplication app,
        Parlamento.Application.Imports.ParliamentDocumentRedactionResult result)
    {
        app.Logger.LogInformation(
            "Parliament document redaction command result. Found={Found} Processed={Processed} Skipped={Skipped} Failed={Failed}",
            result.DocumentsRead,
            result.DocumentsProcessed,
            result.DocumentsSkipped,
            result.DocumentsFailed);
    }

    private sealed class ParliamentDocumentCommandOptions
    {
        public int? ProjectLawId { get; set; }

        public int MaxDocuments { get; set; } = 10;

        public bool ForceUpsert { get; set; }

        public List<string> Legislatures { get; } = [];
    }
}
