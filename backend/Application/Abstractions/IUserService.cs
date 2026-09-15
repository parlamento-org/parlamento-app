using Parlamento.Application.Users;
using Parlamento.Domain.Entities;

namespace Parlamento.Application.Abstractions;

public interface IUserService
{
    Task<IReadOnlyList<User>> SearchAsync(string? searchString, CancellationToken cancellationToken = default);

    Task<ServiceResult<User>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<User>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
