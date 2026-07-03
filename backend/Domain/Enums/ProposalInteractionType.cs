using System.Text.Json.Serialization;

namespace Parlamento.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProposalInteractionType
{
    Impression,
    Support,
    Oppose,
    Skip,
    DetailOpen,
    SourceLinkClick,
    DebateLinkClick,
    PostVoteReveal
}
