using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Auth;
using Parlamento.Domain.Entities;

namespace Parlamento.Infrastructure.Services;

public sealed class JwtTokenService : IAppTokenService
{
    private readonly AppJwtOptions _options;

    public JwtTokenService(IOptions<AppJwtOptions> options)
    {
        _options = options.Value;
    }

    public AppToken CreateToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(Math.Max(1, _options.ExpirationMinutes));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AppToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
