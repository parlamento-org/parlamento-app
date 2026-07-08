using System.Text.Json.Serialization;

namespace Parlamento.Application.Profile;

public sealed class ProfileResponse
{
    [JsonPropertyName("overview")]
    public ProfileOverviewResponse Overview { get; set; } = new();

    [JsonPropertyName("partyAlignment")]
    public PartyAlignmentSectionResponse PartyAlignment { get; set; } = new();
}

public sealed class ProfileOverviewResponse
{
    [JsonPropertyName("proposalsInteracted")]
    public int ProposalsInteracted { get; set; }

    [JsonPropertyName("supportCount")]
    public int SupportCount { get; set; }

    [JsonPropertyName("opposeCount")]
    public int OpposeCount { get; set; }

    [JsonPropertyName("abstentionCount")]
    public int AbstentionCount { get; set; }

    [JsonPropertyName("skipCount")]
    public int SkipCount { get; set; }

    [JsonPropertyName("supportRate")]
    public decimal SupportRate { get; set; }

    [JsonPropertyName("skipRate")]
    public decimal SkipRate { get; set; }
}

public sealed class PartyAlignmentSectionResponse
{
    [JsonPropertyName("isUnlocked")]
    public bool IsUnlocked { get; set; }

    [JsonPropertyName("minimumComparableVotes")]
    public int MinimumComparableVotes { get; set; }

    [JsonPropertyName("totalComparableVotes")]
    public int TotalComparableVotes { get; set; }

    [JsonPropertyName("parties")]
    public List<PartyAlignmentResponse> Parties { get; set; } = [];
}

public sealed class PartyAlignmentResponse
{
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    [JsonPropertyName("partyAcronym")]
    public string PartyAcronym { get; set; } = string.Empty;

    [JsonPropertyName("partyName")]
    public string PartyName { get; set; } = string.Empty;

    [JsonPropertyName("partyLogo")]
    public string? PartyLogo { get; set; }

    [JsonPropertyName("alignedCount")]
    public int AlignedCount { get; set; }

    [JsonPropertyName("comparableCount")]
    public int ComparableCount { get; set; }

    [JsonPropertyName("alignmentPercentage")]
    public decimal AlignmentPercentage { get; set; }
}
