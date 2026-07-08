using System.Text.Json.Serialization;

using Parlamento.Domain.Enums;

namespace Parlamento.Application.ProposalFlow;

public sealed class ProposalHistoryItemResponse
{
    [JsonPropertyName("interactionId")]
    public int InteractionId { get; set; }

    [JsonPropertyName("initiativeId")]
    public int InitiativeId { get; set; }

    [JsonPropertyName("initiativeType")]
    public string InitiativeType { get; set; } = string.Empty;

    [JsonPropertyName("initiativeNumber")]
    public string? InitiativeNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("legislature")]
    public string? Legislature { get; set; }

    [JsonPropertyName("action")]
    public ProposalInteractionType Action { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("proposers")]
    public List<ProposalProposerResponse> Proposers { get; set; } = [];
}

public sealed class ProposalHistoryRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    [JsonPropertyName("page")]
    public int Page { get; set; } = DefaultPage;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = DefaultPageSize;

    [JsonPropertyName("legislature")]
    public string? Legislature { get; set; }

    [JsonPropertyName("proposingParty")]
    public string? ProposingParty { get; set; }

    [JsonPropertyName("search")]
    public string? Search { get; set; }

    [JsonPropertyName("interactionType")]
    public ProposalInteractionType? InteractionType { get; set; }

    public int NormalizedPage => Page < 1 ? DefaultPage : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize
    };

    public string? NormalizedLegislature =>
        string.IsNullOrWhiteSpace(Legislature) ? null : Legislature.Trim();

    public string? NormalizedProposingParty =>
        string.IsNullOrWhiteSpace(ProposingParty) ? null : ProposingParty.Trim();

    public string? NormalizedSearch =>
        string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();

    public ProposalHistoryFilters ToFilters()
    {
        return new ProposalHistoryFilters
        {
            Legislature = NormalizedLegislature,
            ProposingParty = NormalizedProposingParty,
            Search = NormalizedSearch,
            InteractionType = InteractionType
        };
    }
}

public sealed class ProposalHistoryFilters
{
    public string? Legislature { get; set; }

    public string? ProposingParty { get; set; }

    public string? Search { get; set; }

    public ProposalInteractionType? InteractionType { get; set; }
}

public sealed class ProposalHistoryPageResponse
{
    [JsonPropertyName("items")]
    public List<ProposalHistoryItemResponse> Items { get; set; } = [];

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }

    [JsonPropertyName("availableLegislatures")]
    public List<string> AvailableLegislatures { get; set; } = [];

    [JsonPropertyName("availableProposingParties")]
    public List<ProposalHistoryProposingPartyResponse> AvailableProposingParties { get; set; } = [];
}

public sealed class ProposalHistoryProposingPartyResponse
{
    [JsonPropertyName("acronym")]
    public string Acronym { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
