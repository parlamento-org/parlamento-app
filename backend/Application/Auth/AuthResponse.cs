using System.Text.Json.Serialization;

namespace Parlamento.Application.Auth;

public sealed class AuthResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }

    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("user")]
    public AuthenticatedUserResponse User { get; set; } = new();
}
