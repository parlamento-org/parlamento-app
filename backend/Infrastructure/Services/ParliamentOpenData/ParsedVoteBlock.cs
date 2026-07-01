using Parlamento.Domain.Enums;

namespace Parlamento.Infrastructure.Services.ParliamentOpenData;

internal record ParsedVoteBlock(
    VotingOrientation Orientation,
    string? PartyAcronym,
    int? NumberOfDeputies,
    bool? IsUnanimousWithinParty,
    string RawToken,
    string? ParseWarning);
