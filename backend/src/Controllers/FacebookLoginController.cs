using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using backend.Extensions;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Auth;

namespace backend.Controllers;

[ApiController]
[AllowAnonymous]
[Route("/fb-login")]
public class FacebookLoginController : ControllerBase
{
    private readonly IAuthService _authService;

    public FacebookLoginController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost(Name = "ValidateFacebookUser")]
    public async Task<IActionResult> ValidateFacebookUser(FacebookLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.AuthenticateFacebookAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }
}
