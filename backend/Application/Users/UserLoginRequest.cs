using System.Text.Json.Serialization;

namespace Parlamento.Application.Users;

public class UserLoginRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}
