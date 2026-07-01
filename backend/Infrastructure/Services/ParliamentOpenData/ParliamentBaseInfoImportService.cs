using System.Text.Json;
using System.Globalization;
using System.Text;

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
        var parliamentaryGroups = document.RootElement.EnumerateArrayOrEmpty("GruposParlamentares").ToList();
        var upserted = 0;
        var groupsUpserted = 0;

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

        foreach (var groupElement in parliamentaryGroups)
        {
            var acronym = groupElement.GetStringOrNull("sigla")?.Trim();
            if (string.IsNullOrWhiteSpace(acronym))
            {
                continue;
            }

            var group = await _context.ParliamentaryGroups.FirstOrDefaultAsync(
                x => x.Legislature == legislature && x.Acronym == acronym,
                cancellationToken);

            if (group is null)
            {
                group = new ParliamentaryGroup
                {
                    Legislature = legislature,
                    Acronym = acronym
                };
                _context.ParliamentaryGroups.Add(group);
            }

            group.Name = groupElement.GetStringOrNull("nome")?.Trim();
            groupsUpserted++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var oldTerms = _context.ParliamentRedactionTerms.Where(x => x.Legislature == legislature);
        _context.ParliamentRedactionTerms.RemoveRange(oldTerms);

        var terms = BuildRedactionTerms(legislature);
        _context.ParliamentRedactionTerms.AddRange(terms);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Imported parliament base info for {Legislature}. Deputies={Deputies} ParliamentaryGroups={Groups} RedactionTerms={Terms}",
            legislature,
            deputies.Count,
            parliamentaryGroups.Count,
            terms.Count);

        return new ParliamentBaseInfoImportResult(
            legislature,
            deputies.Count,
            upserted,
            parliamentaryGroups.Count,
            groupsUpserted,
            terms.Count);
    }

    private List<ParliamentRedactionTerm> BuildRedactionTerms(string legislature)
    {
        var values = new List<(string Term, string Kind)>();

        foreach (var deputy in _context.ParliamentDeputies.Where(x => x.Legislature == legislature))
        {
            Add(values, deputy.FullName, "DeputyFullName");
            Add(values, deputy.ParliamentaryName, "DeputyParliamentaryName");
        }

        foreach (var group in _context.ParliamentaryGroups.Where(x => x.Legislature == legislature))
        {
            Add(values, group.Acronym, "ParliamentaryGroupAcronym");
            Add(values, group.Name, "ParliamentaryGroupName");
        }

        foreach (var party in _context.PoliticalParties)
        {
            if (IsGovernmentTerm(party.partyAcronym) || IsGovernmentTerm(party.fullName))
            {
                continue;
            }

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

        var trimmed = term.Trim();
        if (IsGovernmentTerm(trimmed))
        {
            return;
        }

        values.Add((trimmed, kind));

        var withoutDiacritics = RemoveDiacritics(trimmed);
        if (!string.Equals(trimmed, withoutDiacritics, StringComparison.Ordinal))
        {
            values.Add((withoutDiacritics, $"{kind}Ascii"));
        }
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool IsGovernmentTerm(string? value)
    {
        return string.Equals(value?.Trim(), "Governo", StringComparison.OrdinalIgnoreCase);
    }
}
