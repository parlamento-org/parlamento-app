using System.Text.Json.Serialization;

namespace Parlamento.Application.Parties;

public class CreatePoliticalPartyRequest
{
    [JsonPropertyName("partyAcronym")]
    public string? PartyAcronym { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("logoLink")]
    public string? LogoLink { get; set; }
}
