using Parlamento.Application.Auth;
using Parlamento.Domain.Entities;

namespace Parlamento.Application.Abstractions;

public interface IAppTokenService
{
    AppToken CreateToken(User user);
}
