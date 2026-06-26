using Microsoft.AspNetCore.Mvc;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Auth;

namespace backend.Controllers;

[ApiController]
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
        var user = await _authService.AuthenticateGoogleAsync(request, cancellationToken);
        return Ok(user);
    }
}
