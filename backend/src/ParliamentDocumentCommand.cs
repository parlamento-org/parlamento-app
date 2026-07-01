using Parlamento.Application.Abstractions;
using Parlamento.Domain.Documents;
using Parlamento.Infrastructure.Persistence;
using Parlamento.Infrastructure.Services.Documents;

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

        if (args.Length >= 2 && string.Equals(args[1], "compare-pdf-extractors", StringComparison.OrdinalIgnoreCase))
        {
            await RunPdfExtractorComparisonAsync(app, args.Skip(2).ToArray());
            return true;
        }

        if (args.Length < 2 || !string.Equals(args[1], "redact", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Expected command: parliament-documents redact or parliament-documents compare-pdf-extractors");
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

    private static async Task RunPdfExtractorComparisonAsync(WebApplication app, string[] args)
    {
        var command = ParseComparison(args);
        var bytes = await File.ReadAllBytesAsync(command.FilePath);
        var sourceName = Path.GetFileName(command.FilePath);

        var pdfPig = new PdfDocumentExtractor();
        var iText = new ITextPdfDocumentExtractor();

        var pdfPigResult = await pdfPig.ExtractAsync(bytes, sourceName);
        var iTextResult = await iText.ExtractAsync(bytes, sourceName);

        var pdfPigText = FlattenDocumentText(pdfPigResult.Document);
        var iTextText = FlattenDocumentText(iTextResult.Document);

        var outputDirectory = Path.GetFullPath(command.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);
        var baseName = Path.GetFileNameWithoutExtension(command.FilePath);
        var pdfPigPath = Path.Combine(outputDirectory, $"{baseName}.pdfpig.txt");
        var iTextPath = Path.Combine(outputDirectory, $"{baseName}.itext.txt");

        await File.WriteAllTextAsync(pdfPigPath, pdfPigText);
        await File.WriteAllTextAsync(iTextPath, iTextText);

        app.Logger.LogInformation(
            "PDF extractor comparison completed. PdfPigChars={PdfPigChars} ITextChars={ITextChars} PdfPigLines={PdfPigLines} ITextLines={ITextLines} PdfPigOutput={PdfPigOutput} ITextOutput={ITextOutput}",
            pdfPigText.Length,
            iTextText.Length,
            CountLines(pdfPigText),
            CountLines(iTextText),
            pdfPigPath,
            iTextPath);

        Console.WriteLine("PDF extractor comparison completed.");
        Console.WriteLine($"PdfPig: {pdfPigText.Length} chars, {CountLines(pdfPigText)} lines -> {pdfPigPath}");
        Console.WriteLine($"iText:  {iTextText.Length} chars, {CountLines(iTextText)} lines -> {iTextPath}");
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

    private static ParliamentDocumentComparisonOptions ParseComparison(string[] args)
    {
        var options = new ParliamentDocumentComparisonOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--file", StringComparison.OrdinalIgnoreCase))
            {
                options.FilePath = RequireValue(args, ref i, "--file");
                continue;
            }

            if (string.Equals(arg, "--output-dir", StringComparison.OrdinalIgnoreCase))
            {
                options.OutputDirectory = RequireValue(args, ref i, "--output-dir");
                continue;
            }

            if (string.IsNullOrWhiteSpace(options.FilePath))
            {
                options.FilePath = arg;
                continue;
            }

            throw new ArgumentException($"Unknown argument for compare-pdf-extractors: {arg}");
        }

        if (string.IsNullOrWhiteSpace(options.FilePath))
        {
            throw new ArgumentException("compare-pdf-extractors requires --file <path>.");
        }

        if (!File.Exists(options.FilePath))
        {
            throw new FileNotFoundException("PDF file was not found.", options.FilePath);
        }

        return options;
    }

    private static string FlattenDocumentText(ParliamentDocumentModel document)
    {
        var lines = new List<string>();
        foreach (var page in document.Pages)
        {
            foreach (var block in page.Blocks)
            {
                AddBlockText(block, lines);
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void AddBlockText(ParliamentDocumentBlock block, List<string> lines)
    {
        if (block.Runs.Count > 0)
        {
            var text = string.Concat(block.Runs.Select(run =>
                run.Kind == ParliamentDocumentRunKind.LineBreak
                    ? Environment.NewLine
                    : run.Text));

            foreach (var line in text.Split(
                         Environment.NewLine,
                         StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                lines.Add(line);
            }
        }

        foreach (var item in block.Items)
        {
            foreach (var child in item.Blocks)
            {
                AddBlockText(child, lines);
            }
        }

        foreach (var row in block.Rows)
        {
            foreach (var cell in row.Cells)
            {
                foreach (var child in cell.Blocks)
                {
                    AddBlockText(child, lines);
                }
            }
        }
    }

    private static int CountLines(string text)
    {
        return text.Split(Environment.NewLine).Length;
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

    private sealed class ParliamentDocumentComparisonOptions
    {
        public string FilePath { get; set; } = string.Empty;

        public string OutputDirectory { get; set; } = Path.Combine("tmp", "pdf-extractor-comparison");
    }
}
