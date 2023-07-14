using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace backend.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProposalResult
{
    RejectedInGenerality,
    RejectedInSpeciality,
    ApprovedInGenerality,
    ApprovedInSpeciality,

}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VotingOrientation
{
    InFavor,
    Against,
    Abstaining,
    NotInterested
}


public class PoliticalParty
{

    [Key]
    [Required]
    public String? partyAcronym { get; set; }
    [Required]
    public String? fullName { get; set; }
    [Required]
    public String? logoLink { get; set; }



}

public class VotingBlock
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Boolean? isUninamousWithinParty { get; set; }

    public int? numberOfDeputies { get; set; }
    [Required]
    public String? politicalPartyAcronym { get; set; }

    [Required]
    [EnumDataType(typeof(VotingOrientation), ErrorMessage = "Invalid Voting Orientation")]
    public VotingOrientation votingOrientation { get; set; }

}

public class VotingResult
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Boolean isUninamous { get; set; }

    public List<VotingBlock>? votingBlocks { get; set; }
}

public class ProjectLawCriteria
{
    [Required]
    public int userID { get; set; }

    public List<String> legislaturas { get; set; } = new List<String>();

    public String? oldestVoteDate { get; set; }

    public String? newestVoteDate { get; set; }

    [Required]
    public int lowestScoreAllowed { get; set; }


}


public class ProjectLawDTO
{


    public String? legislatura { get; set; }

    public int? sourceId { get; set; }

    public int? score { get; set; }
    public String? voteDate { get; set; }


    public String? proposingPartyAcronym { get; set; }


    public String? proposalTitle { get; set; }

    public String? fullProposalTextLink { get; set; }

    public String? proposalTextHTML { get; set; }

    [EnumDataType(typeof(ProposalResult), ErrorMessage = "Invalid Proposal Result")]
    public ProposalResult? proposalResult { get; set; }

    public VotingResult? votingResultGenerality { get; set; }
    public VotingResult? votingResultSpeciality { get; set; }

}

public class ProjectLaw
{

    public override string ToString()
    {
        return "projeto-lei:" + ProposalTitle;
    }

    public ProjectLaw()
    {

    }


    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SourceId { get; set; }

    [Required]
    public String? Legislatura { get; set; }
    [Required]
    public int Score { get; set; }

    [Required]
    public int amountOfUsersInterested { get; set; }

    [Required]
    public int totalAmountOfVotesFromUsers { get; set; }

    [Required]
    public String? VoteDate { get; set; }

    [Required]
    public PoliticalParty? ProposingParty { get; set; }

    [Required]
    public String? ProposalTitle { get; set; }
    [Required]
    public String? FullProposalTextLink { get; set; }

    public String? ProposalTextHTML { get; set; }

    [EnumDataType(typeof(ProposalResult), ErrorMessage = "Invalid Proposal Result")]
    public ProposalResult? ProposalResult { get; set; }

    public VotingResult? VotingResultGenerality { get; set; }
    public VotingResult? VotingResultSpeciality { get; set; }

}