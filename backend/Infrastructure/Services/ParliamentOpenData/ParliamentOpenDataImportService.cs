using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Imports;
using Parlamento.Domain.Entities;
using Parlamento.Domain.Enums;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services.ParliamentOpenData;

public class ParliamentOpenDataImportService : IParliamentOpenDataImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly DatabaseContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ParliamentOpenDataImportService> _logger;

    public ParliamentOpenDataImportService(
        DatabaseContext context,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ParliamentOpenDataImportService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ParliamentImportRunResult> ImportLegislatureAsync(
        string legislature,
        CancellationToken cancellationToken = default)
    {
        var sourceUrl = _configuration[$"ParliamentOpenData:Legislatures:{legislature}:InitiativesUrl"];
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            throw new InvalidOperationException(
                $"No initiative URL is configured for legislature '{legislature}'.");
        }

        _logger.LogInformation(
            "Downloading parliament initiatives for legislature {Legislature} from configured source.",
            legislature);

        using var response = await _httpClient.GetAsync(
            sourceUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await ImportFromStreamAsync(
            "PortugueseParliamentOpenData",
            legislature,
            sourceUrl,
            stream,
            cancellationToken);
    }

    public async Task<ParliamentImportRunResult> ImportFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A file path is required.", nameof(filePath));
        }

        var fullPath = Path.GetFullPath(filePath);
        await using var stream = File.OpenRead(fullPath);

        return await ImportFromStreamAsync(
            "LocalFile",
            null,
            fullPath,
            stream,
            cancellationToken);
    }

    private async Task<ParliamentImportRunResult> ImportFromStreamAsync(
        string sourceKind,
        string? legislature,
        string sourceReference,
        Stream sourceStream,
        CancellationToken cancellationToken)
    {
        var run = new ParliamentImportRun
        {
            SourceKind = sourceKind,
            Legislature = legislature,
            SourceReference = sourceReference,
            StartedAtUtc = DateTime.UtcNow,
            Status = "Running"
        };

        _context.ParliamentImportRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Started parliament import run {RunId}. SourceKind={SourceKind} Legislature={Legislature} SourceReference={SourceReference}",
            run.Id,
            sourceKind,
            legislature,
            sourceReference);

        try
        {
            await foreach (var initiative in ReadInitiativesAsync(sourceStream, cancellationToken))
            {
                run.RecordsRead++;
                try
                {
                    await ImportInitiativeAsync(run, initiative, cancellationToken);
                }
                catch (Exception ex)
                {
                    var sourceId = initiative.GetStringOrNull("IniId");
                    run.RecordsFailed++;
                    _context.ParliamentImportErrors.Add(new ParliamentImportError
                    {
                        ParliamentImportRun = run,
                        SourceId = sourceId,
                        ErrorType = ex.GetType().Name,
                        Message = ex.Message
                    });
                    _logger.LogError(
                        ex,
                        "Failed to import parliament initiative {SourceId}. RunId={RunId}",
                        sourceId,
                        run.Id);
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            run.Status = "Succeeded";
            run.FinishedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Finished parliament import run {RunId}. Read={Read} Inserted={Inserted} Updated={Updated} Skipped={Skipped} Failed={Failed}",
                run.Id,
                run.RecordsRead,
                run.RecordsInserted,
                run.RecordsUpdated,
                run.RecordsSkipped,
                run.RecordsFailed);
            return ToResult(run);
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.FinishedAtUtc = DateTime.UtcNow;
            run.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Parliament import run {RunId} failed.", run.Id);
            throw;
        }
    }

    private async Task ImportInitiativeAsync(
        ParliamentImportRun run,
        JsonElement initiative,
        CancellationToken cancellationToken)
    {
        var sourceIdText = initiative.GetStringOrNull("IniId");
        var initiativeTypeCode = initiative.GetStringOrNull("IniTipo");
        var initiativeTypeDescription = initiative.GetStringOrNull("IniDescTipo");

        if (string.IsNullOrWhiteSpace(sourceIdText) || !int.TryParse(sourceIdText, out var sourceId))
        {
            run.RecordsSkipped++;
            _context.ParliamentImportSkips.Add(new ParliamentImportSkip
            {
                ParliamentImportRun = run,
                SourceId = sourceIdText,
                InitiativeTypeCode = initiativeTypeCode,
                InitiativeTypeDescription = initiativeTypeDescription,
                Reason = "MissingSourceId",
                Message = "IniId is required and must be numeric for compatibility with ProjectLaw.SourceId."
            });
            _logger.LogWarning(
                "Skipped parliament initiative without usable source id. RunId={RunId} SourceId={SourceId} Type={TypeCode}/{TypeDescription}",
                run.Id,
                sourceIdText,
                initiativeTypeCode,
                initiativeTypeDescription);
            return;
        }

        var generalityVote = FindVoteForStage(initiative, "Generality");
        if (generalityVote is null)
        {
            run.RecordsSkipped++;
            _context.ParliamentImportSkips.Add(new ParliamentImportSkip
            {
                ParliamentImportRun = run,
                SourceId = sourceIdText,
                InitiativeTypeCode = initiativeTypeCode,
                InitiativeTypeDescription = initiativeTypeDescription,
                Reason = "MissingGeneralityVote",
                Message = "Initiative has no phase 250 event with a usable Votacao array."
            });
            _logger.LogInformation(
                "Skipped parliament initiative {SourceId}: no usable generality vote. RunId={RunId} Type={TypeCode}/{TypeDescription}",
                sourceIdText,
                run.Id,
                initiativeTypeCode,
                initiativeTypeDescription);
            return;
        }

        var rawJson = initiative.GetRawText();
        var sourceHash = ComputeSha256(rawJson);
        var existing = await FindExistingProjectLawAsync(sourceId, sourceIdText, cancellationToken);

        if (existing is not null && existing.SourceHash == sourceHash)
        {
            run.RecordsSkipped++;
            _logger.LogDebug(
                "Skipped unchanged parliament initiative {SourceId}. RunId={RunId}",
                sourceIdText,
                run.Id);
            return;
        }

        var inserted = existing is null;
        var projectLaw = existing ?? new ProjectLaw
        {
            SourceId = sourceId,
            Score = 100,
            amountOfUsersInterested = 0,
            totalAmountOfVotesFromUsers = 0
        };

        if (inserted)
        {
            _context.ProjectLaws.Add(projectLaw);
        }
        else
        {
            ClearImportedGraph(projectLaw);
        }

        await MapProjectLawAsync(projectLaw, initiative, generalityVote.Value, sourceHash, run, cancellationToken);

        if (_context.Database.IsRelational())
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (inserted)
        {
            run.RecordsInserted++;
            _logger.LogInformation(
                "Inserted parliament initiative {SourceId} into ProjectLaw {ProjectLawId}. RunId={RunId} Legislature={Legislature} Type={TypeCode}/{TypeDescription}",
                sourceIdText,
                projectLaw.Id,
                run.Id,
                projectLaw.Legislatura,
                projectLaw.InitiativeTypeCode,
                projectLaw.InitiativeTypeDescription);
        }
        else
        {
            run.RecordsUpdated++;
            _logger.LogInformation(
                "Updated parliament initiative {SourceId} in ProjectLaw {ProjectLawId}. RunId={RunId} Legislature={Legislature} Type={TypeCode}/{TypeDescription}",
                sourceIdText,
                projectLaw.Id,
                run.Id,
                projectLaw.Legislatura,
                projectLaw.InitiativeTypeCode,
                projectLaw.InitiativeTypeDescription);
        }
    }

    private async Task<ProjectLaw?> FindExistingProjectLawAsync(
        int sourceId,
        string sourceIdText,
        CancellationToken cancellationToken)
    {
        return await _context.ProjectLaws
            .Include(x => x.ProposingParty)
            .Include(x => x.VotingResultGenerality!.votingBlocks)
            .Include(x => x.VotingResultSpeciality!.votingBlocks)
            .Include(x => x.ImportedAuthors)
            .Include(x => x.ImportedEvents)
            .Include(x => x.ImportedVotes)
                .ThenInclude(x => x.Blocks)
            .Include(x => x.ImportedDocuments)
            .Include(x => x.ImportedPublications)
            .Include(x => x.ImportedInterventions)
            .FirstOrDefaultAsync(
                x => x.SourceId == sourceId || x.SourceIdText == sourceIdText,
                cancellationToken);
    }

    private async Task MapProjectLawAsync(
        ProjectLaw projectLaw,
        JsonElement initiative,
        JsonElement generalityVote,
        string sourceHash,
        ParliamentImportRun run,
        CancellationToken cancellationToken)
    {
        var proposingParty = await ResolveProposingPartyAsync(initiative, cancellationToken);

        projectLaw.SourceId = int.Parse(initiative.GetStringOrNull("IniId")!);
        projectLaw.SourceIdText = initiative.GetStringOrNull("IniId");
        projectLaw.SourceHash = sourceHash;
        projectLaw.ImportedAtUtc = DateTime.UtcNow;
        projectLaw.LastImportRun = run;
        projectLaw.Legislatura = initiative.GetStringOrNull("IniLeg") ?? string.Empty;
        projectLaw.InitiativeNumber = initiative.GetStringOrNull("IniNr");
        projectLaw.InitiativeTypeCode = initiative.GetStringOrNull("IniTipo");
        projectLaw.InitiativeTypeDescription = initiative.GetStringOrNull("IniDescTipo");
        projectLaw.InitiativeSelection = initiative.GetStringOrNull("IniSel");
        projectLaw.InitiativeObservations = initiative.GetStringOrNull("IniObs");
        projectLaw.InitiativeTextSubstitution = initiative.GetStringOrNull("IniTextoSubst");
        projectLaw.InitiativeTextSubstitutionField = initiative.GetStringOrNull("IniTextoSubstCampo");
        projectLaw.ProposalTitle = initiative.GetStringOrNull("IniTitulo") ?? string.Empty;
        projectLaw.FullProposalTextLink = initiative.GetStringOrNull("IniLinkTexto") ?? string.Empty;
        projectLaw.ProposingParty = proposingParty;
        projectLaw.VoteDate = generalityVote.GetStringOrNull("data") ?? string.Empty;
        projectLaw.ProposalResult = DetermineProposalResult(initiative, generalityVote);
        ClearVotingResult(projectLaw.VotingResultGenerality, removeResult: false);
        projectLaw.VotingResultGenerality = BuildVotingResult(
            projectLaw.VotingResultGenerality,
            generalityVote);

        var finalVote = FindVoteForStage(initiative, "FinalGlobal") ??
                        FindVoteForStage(initiative, "Speciality");
        ClearVotingResult(projectLaw.VotingResultSpeciality, removeResult: finalVote is null);
        projectLaw.VotingResultSpeciality = finalVote is null
            ? null
            : BuildVotingResult(projectLaw.VotingResultSpeciality, finalVote.Value);

        MapAuthors(projectLaw, initiative);
        MapPrimaryDocument(projectLaw, initiative);
        MapAttachments(projectLaw, null, initiative.EnumerateArrayOrEmpty("IniAnexos"), "InitiativeAttachment");

        foreach (var eventElement in initiative.EnumerateArrayOrEmpty("IniEventos"))
        {
            MapEvent(projectLaw, eventElement);
        }
    }

    private async Task<PoliticalParty> ResolveProposingPartyAsync(
        JsonElement initiative,
        CancellationToken cancellationToken)
    {
        var acronym = initiative
            .EnumerateArrayOrEmpty("IniAutorGruposParlamentares")
            .Select(x => x.GetStringOrNull("GP"))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        acronym ??= initiative.GetObjectOrNull("IniAutorOutros")?.GetStringOrNull("sigla") == "V"
            ? "Governo"
            : initiative.GetObjectOrNull("IniAutorOutros")?.GetStringOrNull("sigla");

        acronym ??= initiative
            .EnumerateArrayOrEmpty("IniAutorDeputados")
            .Select(x => x.GetStringOrNull("GP"))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        acronym = string.IsNullOrWhiteSpace(acronym) ? "Governo" : acronym.Trim();

        var party = await _context.PoliticalParties
            .FirstOrDefaultAsync(x => x.partyAcronym == acronym, cancellationToken);

        if (party is not null)
        {
            return party;
        }

        party = new PoliticalParty
        {
            partyAcronym = acronym,
            fullName = acronym,
            logoLink = string.Empty
        };
        _context.PoliticalParties.Add(party);
        return party;
    }

    private static VotingResult BuildVotingResult(VotingResult? existing, JsonElement vote)
    {
        var result = existing ?? new VotingResult();
        result.isUninamous = !string.IsNullOrWhiteSpace(vote.GetStringOrNull("unanime"));
        result.votingBlocks ??= [];
        result.votingBlocks.Clear();

        foreach (var block in VoteDetailParser.Parse(
            vote.GetStringOrNull("detalhe"),
            vote.GetStringOrNull("unanime"),
            vote.GetRawPropertyOrNull("ausencias"),
            includeUnanimousBlock: false))
        {
            result.votingBlocks.Add(new VotingBlock
            {
                politicalPartyAcronym = block.PartyAcronym ?? block.RawToken,
                numberOfDeputies = block.NumberOfDeputies,
                isUninamousWithinParty = block.IsUnanimousWithinParty,
                votingOrientation = block.Orientation
            });
        }

        return result;
    }

    private static ProposalResult DetermineProposalResult(JsonElement initiative, JsonElement generalityVote)
    {
        var finalVote = FindVoteForStage(initiative, "FinalGlobal") ??
                        FindVoteForStage(initiative, "Speciality");
        var vote = finalVote ?? generalityVote;
        var approved = IsApproved(vote.GetStringOrNull("resultado"));

        if (finalVote is not null)
        {
            return approved ? ProposalResult.ApprovedInSpeciality : ProposalResult.RejectedInSpeciality;
        }

        return approved ? ProposalResult.ApprovedInGenerality : ProposalResult.RejectedInGenerality;
    }

    private void MapAuthors(ProjectLaw projectLaw, JsonElement initiative)
    {
        foreach (var group in initiative.EnumerateArrayOrEmpty("IniAutorGruposParlamentares"))
        {
            projectLaw.ImportedAuthors.Add(new ParliamentInitiativeAuthor
            {
                ProjectLaw = projectLaw,
                AuthorKind = "ParliamentaryGroup",
                Acronym = group.GetStringOrNull("GP")
            });
        }

        foreach (var deputy in initiative.EnumerateArrayOrEmpty("IniAutorDeputados"))
        {
            projectLaw.ImportedAuthors.Add(new ParliamentInitiativeAuthor
            {
                ProjectLaw = projectLaw,
                AuthorKind = "Deputy",
                Acronym = deputy.GetStringOrNull("GP"),
                DeputySourceId = deputy.GetStringOrNull("idCadastro"),
                Name = deputy.GetStringOrNull("nome")
            });
        }

        var other = initiative.GetObjectOrNull("IniAutorOutros");
        if (other is not null && other.Value.ValueKind == JsonValueKind.Object)
        {
            projectLaw.ImportedAuthors.Add(new ParliamentInitiativeAuthor
            {
                ProjectLaw = projectLaw,
                AuthorKind = "Other",
                Acronym = other.Value.GetStringOrNull("sigla"),
                Name = other.Value.GetStringOrNull("nome")
            });
        }
    }

    private static void MapPrimaryDocument(ProjectLaw projectLaw, JsonElement initiative)
    {
        var url = initiative.GetStringOrNull("IniLinkTexto");
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        projectLaw.ImportedDocuments.Add(new ParliamentInitiativeDocument
        {
            ProjectLaw = projectLaw,
            Scope = "InitiativeText",
            Name = "Texto da iniciativa",
            Url = url
        });
    }

    private static void MapAttachments(
        ProjectLaw projectLaw,
        ParliamentInitiativeEvent? parliamentEvent,
        IEnumerable<JsonElement> attachments,
        string scope)
    {
        foreach (var attachment in attachments)
        {
            projectLaw.ImportedDocuments.Add(new ParliamentInitiativeDocument
            {
                ProjectLaw = projectLaw,
                ParliamentInitiativeEvent = parliamentEvent,
                Scope = scope,
                Name = attachment.GetStringOrNull("anexoNome"),
                Url = attachment.GetStringOrNull("anexoFich")
            });
        }
    }

    private static void MapEvent(ProjectLaw projectLaw, JsonElement eventElement)
    {
        var parliamentEvent = new ParliamentInitiativeEvent
        {
            ProjectLaw = projectLaw,
            SourceEventId = eventElement.GetStringOrNull("EvtId"),
            SourceActivityId = eventElement.GetStringOrNull("ActId"),
            SourceObjectEventId = eventElement.GetStringOrNull("OevId"),
            SourceTextId = eventElement.GetStringOrNull("OevTextId"),
            PhaseCode = eventElement.GetStringOrNull("CodigoFase"),
            PhaseName = eventElement.GetStringOrNull("Fase"),
            PhaseDate = eventElement.GetStringOrNull("DataFase"),
            Observation = eventElement.GetStringOrNull("ObsFase"),
            ApprovedTextId = eventElement.GetStringOrNull("TextosAprovados")
        };

        projectLaw.ImportedEvents.Add(parliamentEvent);

        MapAttachments(projectLaw, parliamentEvent, eventElement.EnumerateArrayOrEmpty("AnexosFase"), "EventAttachment");
        MapPublications(projectLaw, parliamentEvent, eventElement.EnumerateArrayOrEmpty("PublicacaoFase"), "EventPublication");
        MapVotes(projectLaw, parliamentEvent, eventElement.EnumerateArrayOrEmpty("Votacao"));
        MapCommissions(projectLaw, parliamentEvent, eventElement.EnumerateArrayOrEmpty("Comissao"));
        MapInterventions(projectLaw, parliamentEvent, eventElement.EnumerateArrayOrEmpty("Intervencoesdebates"));
    }

    private static void MapVotes(
        ProjectLaw projectLaw,
        ParliamentInitiativeEvent parliamentEvent,
        IEnumerable<JsonElement> votes)
    {
        var stage = StageFromPhase(parliamentEvent.PhaseCode);

        foreach (var voteElement in votes)
        {
            var vote = new ParliamentInitiativeVote
            {
                ProjectLaw = projectLaw,
                ParliamentInitiativeEvent = parliamentEvent,
                SourceVoteId = voteElement.GetStringOrNull("id"),
                Stage = stage,
                VoteDate = voteElement.GetStringOrNull("data"),
                Description = voteElement.GetStringOrNull("descricao"),
                Result = voteElement.GetStringOrNull("resultado"),
                Unanimous = voteElement.GetStringOrNull("unanime"),
                Detail = voteElement.GetStringOrNull("detalhe"),
                Meeting = voteElement.GetStringOrNull("reuniao"),
                MeetingType = voteElement.GetStringOrNull("tipoReuniao")
            };

            foreach (var block in VoteDetailParser.Parse(
                         vote.Detail,
                         vote.Unanimous,
                         voteElement.GetRawPropertyOrNull("ausencias")))
            {
                vote.Blocks.Add(new ParliamentInitiativeVoteBlock
                {
                    ParliamentInitiativeVote = vote,
                    PartyAcronym = block.PartyAcronym,
                    NumberOfDeputies = block.NumberOfDeputies,
                    IsUnanimousWithinParty = block.IsUnanimousWithinParty,
                    VotingOrientation = block.Orientation,
                    RawToken = block.RawToken,
                    ParseWarning = block.ParseWarning
                });
            }

            projectLaw.ImportedVotes.Add(vote);
            parliamentEvent.Votes.Add(vote);
        }
    }

    private static void MapPublications(
        ProjectLaw projectLaw,
        ParliamentInitiativeEvent? parliamentEvent,
        IEnumerable<JsonElement> publications,
        string scope)
    {
        foreach (var publication in publications)
        {
            projectLaw.ImportedPublications.Add(new ParliamentInitiativePublication
            {
                ProjectLaw = projectLaw,
                ParliamentInitiativeEvent = parliamentEvent,
                Scope = scope,
                PublicationDate = publication.GetStringOrNull("pubdt"),
                Legislature = publication.GetStringOrNull("pubLeg"),
                Number = publication.GetStringOrNull("pubNr"),
                Series = publication.GetStringOrNull("pubSL"),
                Type = publication.GetStringOrNull("pubTipo"),
                TypeCode = publication.GetStringOrNull("pubTp"),
                DiaryUrl = publication.GetStringOrNull("URLDiario")
            });
        }
    }

    private static void MapCommissions(ProjectLaw projectLaw, ParliamentInitiativeEvent parliamentEvent, IEnumerable<JsonElement> commissions)
    {
        foreach (var commission in commissions)
        {
            foreach (var document in commission.EnumerateArrayOrEmpty("Documentos"))
            {
                projectLaw.ImportedDocuments.Add(new ParliamentInitiativeDocument
                {
                    ProjectLaw = projectLaw,
                    ParliamentInitiativeEvent = parliamentEvent,
                    Scope = "CommitteeDocument",
                    Name = document.GetStringOrNull("TituloDocumento"),
                    DocumentType = document.GetStringOrNull("TipoDocumento"),
                    DocumentDate = document.GetStringOrNull("DataDocumento"),
                    Url = document.GetStringOrNull("URL"),
                    CommitteeId = commission.GetStringOrNull("IdComissao"),
                    CommitteeName = commission.GetStringOrNull("Nome")
                });
            }

            MapVotes(projectLaw, parliamentEvent, commission.EnumerateArrayOrEmpty("Votacao"));
        }
    }

    private static void MapInterventions(
        ProjectLaw projectLaw,
        ParliamentInitiativeEvent parliamentEvent,
        IEnumerable<JsonElement> interventions)
    {
        foreach (var intervention in interventions)
        {
            var meetingDate = intervention.GetStringOrNull("dataReuniaoPlenaria");

            foreach (var speaker in intervention.EnumerateArrayOrEmpty("oradores"))
            {
                var deputy = speaker.EnumerateArrayOrEmpty("deputadosOradores").FirstOrDefault();
                var governmentMember = speaker.GetObjectOrNull("membrosGoverno");

                projectLaw.ImportedInterventions.Add(new ParliamentInitiativeIntervention
                {
                    ProjectLaw = projectLaw,
                    ParliamentInitiativeEvent = parliamentEvent,
                    PlenaryMeetingDate = meetingDate,
                    SpeakerName = deputy.ValueKind == JsonValueKind.Object ? deputy.GetStringOrNull("nome") : null,
                    SpeakerParty = deputy.ValueKind == JsonValueKind.Object ? deputy.GetStringOrNull("GP") : null,
                    GovernmentMemberName = governmentMember is { ValueKind: JsonValueKind.Object }
                        ? governmentMember.Value.GetStringOrNull("nome")
                        : null,
                    GovernmentMemberRole = governmentMember is { ValueKind: JsonValueKind.Object }
                        ? governmentMember.Value.GetStringOrNull("cargo")
                        : null,
                    StartTime = speaker.GetStringOrNull("horaInicio"),
                    EndTime = speaker.GetStringOrNull("horaTermo"),
                    VideoUrl = speaker
                        .EnumerateArrayOrEmpty("linkVideo")
                        .Select(x => x.GetStringOrNull("link"))
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                    PublicationDiaryUrl = speaker
                        .EnumerateArrayOrEmpty("publicacao")
                        .Select(x => x.GetStringOrNull("URLDiario"))
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                });
            }
        }
    }

    private void ClearImportedGraph(ProjectLaw projectLaw)
    {
        _context.ParliamentInitiativeAuthors.RemoveRange(projectLaw.ImportedAuthors);
        _context.ParliamentInitiativeVoteBlocks.RemoveRange(projectLaw.ImportedVotes.SelectMany(x => x.Blocks));
        _context.ParliamentInitiativeVotes.RemoveRange(projectLaw.ImportedVotes);
        _context.ParliamentInitiativeDocuments.RemoveRange(projectLaw.ImportedDocuments);
        _context.ParliamentInitiativePublications.RemoveRange(projectLaw.ImportedPublications);
        _context.ParliamentInitiativeInterventions.RemoveRange(projectLaw.ImportedInterventions);
        _context.ParliamentInitiativeEvents.RemoveRange(projectLaw.ImportedEvents);

        projectLaw.ImportedAuthors.Clear();
        projectLaw.ImportedVotes.Clear();
        projectLaw.ImportedDocuments.Clear();
        projectLaw.ImportedPublications.Clear();
        projectLaw.ImportedInterventions.Clear();
        projectLaw.ImportedEvents.Clear();
    }

    private void ClearVotingResult(VotingResult? votingResult, bool removeResult)
    {
        if (votingResult is null)
        {
            return;
        }

        if (votingResult.votingBlocks is { Count: > 0 })
        {
            _context.Set<VotingBlock>().RemoveRange(votingResult.votingBlocks);
            votingResult.votingBlocks.Clear();
        }

        if (removeResult)
        {
            _context.Set<VotingResult>().Remove(votingResult);
        }
    }

    private static JsonElement? FindVoteForStage(JsonElement initiative, string stage)
    {
        var votes = initiative
            .EnumerateArrayOrEmpty("IniEventos")
            .Where(x => StageFromPhase(x.GetStringOrNull("CodigoFase")) == stage)
            .SelectMany(x => x.EnumerateArrayOrEmpty("Votacao"))
            .Where(x => x.ValueKind == JsonValueKind.Object)
            .ToList();

        return votes.Count == 0 ? null : votes[^1];
    }

    private static string StageFromPhase(string? phaseCode)
    {
        return phaseCode switch
        {
            "250" => "Generality",
            "310" => "Speciality",
            "320" => "FinalGlobal",
            "370" or "380" or "390" or "400" => "PostApproval",
            "580" => "PublishedLaw",
            _ => "Other"
        };
    }

    private static bool IsApproved(string? result)
    {
        return Normalize(result) is "aprovado" or "aprovada";
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace("á", "a")
            .Replace("à", "a")
            .Replace("ã", "a")
            .Replace("â", "a")
            .Replace("ç", "c")
            .Replace("é", "e")
            .Replace("ê", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("õ", "o")
            .Replace("ú", "u");
    }

    private async IAsyncEnumerable<JsonElement> ReadInitiativesAsync(
        Stream stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (readableStream, firstToken) = await PrepareReadableStreamAsync(stream, cancellationToken);

        if (firstToken == '[')
        {
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(
                               readableStream,
                               JsonOptions,
                               cancellationToken))
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    yield return item;
                }
            }

            yield break;
        }

        if (firstToken == '{')
        {
            using var document = await JsonDocument.ParseAsync(readableStream, cancellationToken: cancellationToken);
            yield return document.RootElement.Clone();
            yield break;
        }

        throw new JsonException("Expected initiative JSON root to be an object or array.");
    }

    private static async Task<(Stream Stream, char FirstToken)> PrepareReadableStreamAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            var originalPosition = stream.Position;
            var first = await ReadFirstNonWhitespaceByteAsync(stream, cancellationToken);
            stream.Position = originalPosition;
            return (stream, (char)first);
        }

        var prefix = new MemoryStream();
        int next;
        do
        {
            next = stream.ReadByte();
            if (next < 0)
            {
                throw new JsonException("The JSON stream is empty.");
            }

            prefix.WriteByte((byte)next);
        }
        while (char.IsWhiteSpace((char)next));

        prefix.Position = 0;
        return (new PrefixStream(prefix, stream), (char)next);
    }

    private static async Task<int> ReadFirstNonWhitespaceByteAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        while (await stream.ReadAsync(buffer, cancellationToken) == 1)
        {
            if (!char.IsWhiteSpace((char)buffer[0]))
            {
                return buffer[0];
            }
        }

        throw new JsonException("The JSON stream is empty.");
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static ParliamentImportRunResult ToResult(ParliamentImportRun run)
    {
        return new ParliamentImportRunResult(
            run.Id,
            run.Status,
            run.RecordsRead,
            run.RecordsInserted,
            run.RecordsUpdated,
            run.RecordsSkipped,
            run.RecordsFailed,
            run.ErrorMessage);
    }
}
