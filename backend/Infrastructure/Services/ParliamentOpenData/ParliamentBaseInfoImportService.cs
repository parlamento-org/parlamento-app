using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Imports;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services.ParliamentOpenData;

public class ParliamentBaseInfoImportService : IParliamentBaseInfoImportService
{
    private readonly DatabaseContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ParliamentBaseInfoImportService> _logger;

    public ParliamentBaseInfoImportService(
        DatabaseContext context,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ParliamentBaseInfoImportService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ParliamentBaseInfoImportResult> ImportLegislatureAsync(
        string legislature,
        CancellationToken cancellationToken = default)
    {
        var sourceUrl = _configuration[$"ParliamentOpenData:Legislatures:{legislature}:BaseInfoUrl"];
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            throw new InvalidOperationException(
                $"No base information URL is configured for legislature '{legislature}'.");
        }

        using var response = await _httpClient.GetAsync(
            sourceUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await ImportFromStreamAsync(legislature, stream, cancellationToken);
    }

    public async Task<ParliamentBaseInfoImportResult> ImportFromFileAsync(
        string legislature,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(Path.GetFullPath(filePath));
        return await ImportFromStreamAsync(legislature, stream, cancellationToken);
    }

    private async Task<ParliamentBaseInfoImportResult> ImportFromStreamAsync(
        string legislature,
        Stream stream,
        CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var deputies = document.RootElement.EnumerateArrayOrEmpty("Deputados").ToList();
        var upserted = 0;

        foreach (var deputyElement in deputies)
        {
            var sourceDeputyId = deputyElement.GetStringOrNull("DepId");
            var sourceCadId = deputyElement.GetStringOrNull("DepCadId");
            var actualLegislature = deputyElement.GetStringOrNull("LegDes") ?? legislature;

            var deputy = await _context.ParliamentDeputies.FirstOrDefaultAsync(
                x => x.Legislature == actualLegislature &&
                     (x.SourceDeputyId == sourceDeputyId || x.SourceCadId == sourceCadId),
                cancellationToken);

            if (deputy is null)
            {
                deputy = new ParliamentDeputy();
                _context.ParliamentDeputies.Add(deputy);
            }

            deputy.Legislature = actualLegislature;
            deputy.SourceDeputyId = sourceDeputyId;
            deputy.SourceCadId = sourceCadId;
            deputy.FullName = deputyElement.GetStringOrNull("DepNomeCompleto");
            deputy.ParliamentaryName = deputyElement.GetStringOrNull("DepNomeParlamentar");
            deputy.Constituency = deputyElement.GetStringOrNull("DepCPDes");
            deputy.PartyAcronym = deputyElement
                .EnumerateArrayOrEmpty("DepGP")
                .Select(x => x.GetStringOrNull("gpSigla"))
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            deputy.Situation = deputyElement
                .EnumerateArrayOrEmpty("DepSituacao")
                .Select(x => x.GetStringOrNull("sioDes"))
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            upserted++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var oldTerms = _context.ParliamentRedactionTerms.Where(x => x.Legislature == legislature);
        _context.ParliamentRedactionTerms.RemoveRange(oldTerms);

        var terms = BuildRedactionTerms(legislature);
        _context.ParliamentRedactionTerms.AddRange(terms);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Imported parliament base info for {Legislature}. Deputies={Deputies} RedactionTerms={Terms}",
            legislature,
            deputies.Count,
            terms.Count);

        return new ParliamentBaseInfoImportResult(legislature, deputies.Count, upserted, terms.Count);
    }

    private List<ParliamentRedactionTerm> BuildRedactionTerms(string legislature)
    {
        var values = new List<(string Term, string Kind)>();

        foreach (var deputy in _context.ParliamentDeputies.Where(x => x.Legislature == legislature))
        {
            Add(values, deputy.FullName, "DeputyFullName");
            Add(values, deputy.ParliamentaryName, "DeputyParliamentaryName");
        }

        foreach (var party in _context.PoliticalParties)
        {
            Add(values, party.partyAcronym, "PartyAcronym");
            Add(values, party.fullName, "PartyName");
        }

        return values
            .GroupBy(x => x.Term.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(x => new ParliamentRedactionTerm
            {
                Legislature = legislature,
                Term = x.Key,
                TermKind = x.First().Kind
            })
            .ToList();
    }

    private static void Add(List<(string Term, string Kind)> values, string? term, string kind)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
        {
            return;
        }

        values.Add((term.Trim(), kind));
    }
}
