using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Parlamento.Application.Abstractions;

namespace Parlamento.Infrastructure.Services.ParliamentOpenData;

public class DailyParliamentImportHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<ParliamentOpenDataOptions> _options;
    private readonly ILogger<DailyParliamentImportHostedService> _logger;

    public DailyParliamentImportHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<ParliamentOpenDataOptions> options,
        ILogger<DailyParliamentImportHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            if (!options.DailyImport.Enabled)
            {
                _logger.LogInformation("Daily parliament import job is disabled.");
                return;
            }

            var delay = GetDelayUntilNextRun(options.DailyImport);
            _logger.LogInformation(
                "Next parliament import job scheduled in {Delay} at local time {RunAt}.",
                delay,
                options.DailyImport.RunAt);

            await Task.Delay(delay, stoppingToken);

            var legislature = options.LatestLegislature;
            if (string.IsNullOrWhiteSpace(legislature))
            {
                _logger.LogWarning("Daily parliament import skipped because ParliamentOpenData:LatestLegislature is not configured.");
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var importService = scope.ServiceProvider.GetRequiredService<IParliamentOpenDataImportService>();

                _logger.LogInformation("Starting daily parliament import for latest legislature {Legislature}.", legislature);
                var result = await importService.ImportLegislatureAsync(legislature, stoppingToken);
                _logger.LogInformation(
                    "Daily parliament import finished. RunId={RunId} Status={Status} Read={Read} Inserted={Inserted} Updated={Updated} Skipped={Skipped} Failed={Failed}",
                    result.RunId,
                    result.Status,
                    result.RecordsRead,
                    result.RecordsInserted,
                    result.RecordsUpdated,
                    result.RecordsSkipped,
                    result.RecordsFailed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily parliament import failed.");
            }
        }
    }

    private static TimeSpan GetDelayUntilNextRun(DailyParliamentImportOptions options)
    {
        var timeZone = ResolveTimeZone(options.TimeZoneId);
        var nowUtc = DateTimeOffset.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, timeZone);
        var nextLocal = nowLocal.Date + options.RunAt;

        if (nextLocal <= nowLocal)
        {
            nextLocal = nextLocal.AddDays(1);
        }

        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified), timeZone);
        return nextUtc - nowUtc;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException) when (timeZoneId == "Europe/Lisbon")
        {
            return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        }
    }
}
