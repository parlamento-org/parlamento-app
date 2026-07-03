using System.Text.Json.Serialization;

using Parlamento.Domain.Entities;

namespace Parlamento.Application.Auth;

public sealed class AuthenticatedUserResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("profilePic")]
    public int ProfilePic { get; set; }

    [JsonPropertyName("partyStats")]
    public List<PartyStats> PartyStats { get; set; } = [];

    [JsonPropertyName("votes")]
    public List<Vote> Votes { get; set; } = [];

    public static AuthenticatedUserResponse FromUser(User user)
    {
        return new AuthenticatedUserResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            ProfilePic = user.ProfilePic,
            PartyStats = user.PartyStats,
            Votes = user.Votes
        };
    }
}
