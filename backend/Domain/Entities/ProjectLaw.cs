using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Parlamento.Domain.Enums;

namespace Parlamento.Domain.Entities;

public class ProjectLaw
{
    public override string ToString()
    {
        return "projeto-lei:" + ProposalTitle;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SourceId { get; set; }

    [Required]
    public string? Legislatura { get; set; }

    [Required]
    public int Score { get; set; }

    [Required]
    public int amountOfUsersInterested { get; set; }

    [Required]
    public int totalAmountOfVotesFromUsers { get; set; }

    [Required]
    public string? VoteDate { get; set; }

    [Required]
    public PoliticalParty? ProposingParty { get; set; }

    [Required]
    public string? ProposalTitle { get; set; }

    [Required]
    public string? FullProposalTextLink { get; set; }

    public string? ProposalTextHTML { get; set; }

    [EnumDataType(typeof(ProposalResult), ErrorMessage = "Invalid Proposal Result")]
    public ProposalResult? ProposalResult { get; set; }

    public VotingResult? VotingResultGenerality { get; set; }

    public VotingResult? VotingResultSpeciality { get; set; }
}
