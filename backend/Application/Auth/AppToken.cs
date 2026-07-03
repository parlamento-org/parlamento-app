namespace Parlamento.Application.Auth;

public sealed record AppToken(string AccessToken, DateTime ExpiresAtUtc);
