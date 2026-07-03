using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using backend.Extensions;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Auth;

namespace backend.Controllers;

[ApiController]
[AllowAnonymous]
[Route("/google-login")]
public class GoogleLoginController : ControllerBase
{
    private readonly IAuthService _authService;

    public GoogleLoginController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost(Name = "ValidateGoogleUser")]
    public async Task<IActionResult> ValidateGoogleUser(GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.AuthenticateGoogleAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }
}
