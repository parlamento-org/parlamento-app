using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using Parlamento.Domain.Enums;

namespace Parlamento.Domain.Entities;

public class ParliamentInitiativeVoteBlock
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ParliamentInitiativeVoteId { get; set; }

    [JsonIgnore]
    public ParliamentInitiativeVote? ParliamentInitiativeVote { get; set; }

    public string? PartyAcronym { get; set; }

    public int? NumberOfDeputies { get; set; }

    public bool? IsUnanimousWithinParty { get; set; }

    [Required]
    public VotingOrientation VotingOrientation { get; set; }

    public string? RawToken { get; set; }

    public string? ParseWarning { get; set; }
}
