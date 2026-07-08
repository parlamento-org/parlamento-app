using Parlamento.Application.Auth;
using Parlamento.Application.Users;

namespace Parlamento.Application.Abstractions;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> LoginAsync(UserLoginRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<AuthResponse>> AuthenticateGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<AuthResponse>> AuthenticateFacebookAsync(FacebookLoginRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<AuthResponse>> GetSessionAsync(int userId, CancellationToken cancellationToken = default);
}
