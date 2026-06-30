using System.Text.Json.Serialization;

namespace Parlamento.Application.Auth;

public class GoogleLoginRequest
{
    [JsonPropertyName("googleIDToken")]
    public string? GoogleIdToken { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("profilePic")]
    public int ProfilePic { get; set; }
}
