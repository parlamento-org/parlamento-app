using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using backend.Extensions;

using Parlamento.Application.Abstractions;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("session", Name = "GetCurrentSession")]
    public async Task<IActionResult> GetSession(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _authService.GetSessionAsync(userId.Value, cancellationToken);
        return this.ToActionResult(result);
    }
}
