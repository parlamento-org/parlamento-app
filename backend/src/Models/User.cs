using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public String? UserName { get; set; }

    [Required]
    public String? Email { get; set; }

    [Required]
    public String? Password { get; set; }

    [Required]
    public int ProfilePic { get; set; }

    public List<Vote> Votes { get; set; } = new List<Vote>();

    public List<PartyStats> PartyStats { get; set; } = new List<PartyStats>();
}

public class Vote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public DateTime VoteDate { get; set; }

    [Required]
    public ProjectLaw? ProjectLaw { get; set; }

    [Required]
    [EnumDataType(typeof(VotingOrientation), ErrorMessage = "Invalid Voting Orientation")]
    VotingOrientation VotingOrientation { get; set; }

}

public class PartyStats
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public PoliticalParty? PoliticalParty { get; set; }
    [Required]
    public int PartyAffection { get; set; }
}