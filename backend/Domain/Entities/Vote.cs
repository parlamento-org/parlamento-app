using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Parlamento.Domain.Enums;

namespace Parlamento.Domain.Entities;

public class Vote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public DateTime VoteDate { get; set; }

    [Required]
    public int ProjectLawID { get; set; }

    [Required]
    [EnumDataType(typeof(VotingOrientation), ErrorMessage = "Invalid Voting Orientation")]
    public VotingOrientation VotingOrientation { get; set; }
}
