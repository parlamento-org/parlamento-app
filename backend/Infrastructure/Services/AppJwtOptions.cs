namespace Parlamento.Infrastructure.Services;

public sealed class AppJwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; set; } = "parlamento-app";

    public string Audience { get; set; } = "parlamento-app";

    public string SigningKey { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;
}
