using Parlamento.Application.Auth;
using Parlamento.Application.Users;
using Parlamento.Domain.Entities;

namespace Parlamento.Application.Abstractions;

public interface IAuthService
{
    Task<ServiceResult<User>> LoginAsync(UserLoginRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<User>> AuthenticateGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<User>> AuthenticateFacebookAsync(FacebookLoginRequest request, CancellationToken cancellationToken = default);
}
