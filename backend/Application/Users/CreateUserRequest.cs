using System.Text.Json.Serialization;

namespace Parlamento.Application.Users;

public class CreateUserRequest
{
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("profilePic")]
    public int ProfilePic { get; set; }
}
