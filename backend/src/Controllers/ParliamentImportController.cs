using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Imports;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("parliament-import")]
public class ParliamentImportController : ControllerBase
{
    private readonly IParliamentOpenDataImportService _importService;
    private readonly IParliamentBaseInfoImportService _baseInfoImportService;
    private readonly IParliamentDocumentRedactionService _documentRedactionService;

    public ParliamentImportController(
        IParliamentOpenDataImportService importService,
        IParliamentBaseInfoImportService baseInfoImportService,
        IParliamentDocumentRedactionService documentRedactionService)
    {
        _importService = importService;
        _baseInfoImportService = baseInfoImportService;
        _documentRedactionService = documentRedactionService;
    }

    [HttpPost("legislatures/{legislature}")]
    public async Task<ActionResult<ParliamentImportRunResult>> ImportLegislature(
        string legislature,
        CancellationToken cancellationToken)
    {
        var result = await _importService.ImportLegislatureAsync(legislature, cancellationToken);
        return Ok(result);
    }

    [HttpPost("base-info/{legislature}")]
    public async Task<ActionResult<ParliamentBaseInfoImportResult>> ImportBaseInfo(
        string legislature,
        CancellationToken cancellationToken)
    {
        var result = await _baseInfoImportService.ImportLegislatureAsync(legislature, cancellationToken);
        return Ok(result);
    }

    [HttpPost("base-info/{legislature}/local-file")]
    public async Task<ActionResult<ParliamentBaseInfoImportResult>> ImportBaseInfoLocalFile(
        string legislature,
        ImportLocalFileRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            return BadRequest("FilePath is required.");
        }

        var result = await _baseInfoImportService.ImportFromFileAsync(legislature, request.FilePath, cancellationToken);
        return Ok(result);
    }

    [HttpPost("documents/redact")]
    public async Task<ActionResult<ParliamentDocumentRedactionResult>> RedactDocuments(
        RedactDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _documentRedactionService.ProcessInitiativeTextDocumentsAsync(
            request.Legislature,
            request.ProjectLawId,
            request.MaxDocuments,
            request.ForceUpsert,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("legislatures")]
    public async Task<ActionResult<IReadOnlyList<ParliamentImportRunResult>>> ImportLegislatures(
        ImportLegislaturesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Legislatures.Count == 0)
        {
            return BadRequest("At least one legislature is required.");
        }

        var results = new List<ParliamentImportRunResult>();
        foreach (var legislature in request.Legislatures.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
        {
            results.Add(await _importService.ImportLegislatureAsync(legislature, cancellationToken));
        }

        return Ok(results);
    }

    [HttpPost("local-file")]
    public async Task<ActionResult<ParliamentImportRunResult>> ImportLocalFile(
        ImportLocalFileRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            return BadRequest("FilePath is required.");
        }

        var result = await _importService.ImportFromFileAsync(request.FilePath, cancellationToken);
        return Ok(result);
    }
}
