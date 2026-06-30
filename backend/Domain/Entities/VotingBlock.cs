using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Parlamento.Domain.Enums;

namespace Parlamento.Domain.Entities;

public class VotingBlock
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public bool? isUninamousWithinParty { get; set; }

    public int? numberOfDeputies { get; set; }

    [Required]
    public string? politicalPartyAcronym { get; set; }

    [Required]
    [EnumDataType(typeof(VotingOrientation), ErrorMessage = "Invalid Voting Orientation")]
    public VotingOrientation votingOrientation { get; set; }
}
