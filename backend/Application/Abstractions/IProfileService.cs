using Parlamento.Application.Profile;

namespace Parlamento.Application.Abstractions;

public interface IProfileService
{
    Task<ServiceResult<ProfileResponse>> GetProfileAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
