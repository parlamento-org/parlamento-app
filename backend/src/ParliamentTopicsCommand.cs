using Parlamento.Application.Abstractions;

internal static class ParliamentTopicsCommand
{
    public static async Task<bool> TryRunAsync(WebApplication app, string[] args)
    {
        if (args.Length == 0 ||
            !string.Equals(args[0], "parliament-topics", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (args.Length < 2 ||
            (!string.Equals(args[1], "seed-reviewed", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(args[1], "seed", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Expected command: parliament-topics seed-reviewed");
        }

        using var scope = app.Services.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<IProposalTopicTaxonomyImportService>();

        app.Logger.LogInformation("Running reviewed proposal topic taxonomy seed command.");
        var result = await importService.ImportReviewedTaxonomyAsync();
        app.Logger.LogInformation(
            "Reviewed proposal topic taxonomy seed command result. ParentsRead={ParentsRead} ParentsInserted={ParentsInserted} ParentsUpdated={ParentsUpdated} SubtopicsRead={SubtopicsRead} SubtopicsInserted={SubtopicsInserted} SubtopicsUpdated={SubtopicsUpdated} SeedAssignmentsRead={SeedAssignmentsRead} SeedAssignmentsInserted={SeedAssignmentsInserted} SeedAssignmentsUpdated={SeedAssignmentsUpdated} SeedAssignmentsSkipped={SeedAssignmentsSkipped} MissingProjectLaws={MissingProjectLaws} MissingSubtopics={MissingSubtopics}",
            result.ParentTopicsRead,
            result.ParentTopicsInserted,
            result.ParentTopicsUpdated,
            result.SubtopicsRead,
            result.SubtopicsInserted,
            result.SubtopicsUpdated,
            result.SeedAssignmentsRead,
            result.SeedAssignmentsInserted,
            result.SeedAssignmentsUpdated,
            result.SeedAssignmentsSkipped,
            result.SeedAssignmentsMissingProjectLaws,
            result.SeedAssignmentsMissingSubtopics);

        Console.WriteLine("Reviewed proposal topic taxonomy seed completed.");
        Console.WriteLine($"Parents: read={result.ParentTopicsRead}, inserted={result.ParentTopicsInserted}, updated={result.ParentTopicsUpdated}");
        Console.WriteLine($"Subtopics: read={result.SubtopicsRead}, inserted={result.SubtopicsInserted}, updated={result.SubtopicsUpdated}");
        Console.WriteLine($"Reviewed assignments: read={result.SeedAssignmentsRead}, inserted={result.SeedAssignmentsInserted}, updated={result.SeedAssignmentsUpdated}, skipped={result.SeedAssignmentsSkipped}, missingProjectLaws={result.SeedAssignmentsMissingProjectLaws}, missingSubtopics={result.SeedAssignmentsMissingSubtopics}");

        return true;
    }
}
