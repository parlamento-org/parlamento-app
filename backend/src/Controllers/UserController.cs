using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using backend.Extensions;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Users;
using Parlamento.Application.Votes;
using Parlamento.Domain.Entities;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("/user")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet(Name = "GetUsers")]
    public async Task<Dictionary<string, List<User>>> Get(string? searchString, CancellationToken cancellationToken)
    {
        var users = await _userService.SearchAsync(searchString, cancellationToken);
        return new Dictionary<string, List<User>>
        {
            ["users"] = users.ToList()
        };
    }

    [HttpPost(Name = "CreateUser")]
    [AllowAnonymous]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut(Name = "AddUserVote")]
    public async Task<IActionResult> Vote(VoteRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        request.UserId = userId.Value;
        var result = await _userService.AddVoteAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{id}", Name = "DeleteUser")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _userService.DeleteAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }
}
