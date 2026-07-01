using Microsoft.AspNetCore.Mvc;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Imports;

namespace backend.Controllers;

[ApiController]
[Route("parliament-import")]
public class ParliamentImportController : ControllerBase
{
    private readonly IParliamentOpenDataImportService _importService;

    public ParliamentImportController(IParliamentOpenDataImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("legislatures/{legislature}")]
    public async Task<ActionResult<ParliamentImportRunResult>> ImportLegislature(
        string legislature,
        CancellationToken cancellationToken)
    {
        var result = await _importService.ImportLegislatureAsync(legislature, cancellationToken);
        return Ok(result);
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
