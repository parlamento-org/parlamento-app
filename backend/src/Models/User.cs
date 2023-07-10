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

public class UserValidateDTO
{

    [Required]
    public String? Email { get; set; }

    [Required]
    public String? Password { get; set; }

}

public class UserDTO
{

    [Required]
    public String? userName { get; set; }

    [Required]
    public String? email { get; set; }

    [Required]
    public String? password { get; set; }

    [Required]
    public int profilePic { get; set; }

}



public class Vote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public DateTime VoteDate { get; set; }

    [Required]
    public ProjectLaw? ProjectLaw { get; set; }

    [Required]
    [EnumDataType(typeof(VotingOrientation), ErrorMessage = "Invalid Voting Orientation")]
    public VotingOrientation VotingOrientation { get; set; }

}

public class VoteDTO
{

    [Required]
    public int userID { get; set; }
    [Required]
    public String? voteDate { get; set; }

    [Required]
    public int projectLawID { get; set; }

    [Required]
    [EnumDataType(typeof(VotingOrientation), ErrorMessage = "Invalid Voting Orientation")]
    public VotingOrientation votingOrientation { get; set; }

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