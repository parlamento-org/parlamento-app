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

    public string? SourceIdText { get; set; }

    public string? SourceHash { get; set; }

    public DateTime? ImportedAtUtc { get; set; }

    public int? LastImportRunId { get; set; }

    public ParliamentImportRun? LastImportRun { get; set; }

    [Required]
    public string? Legislatura { get; set; }

    public string? InitiativeNumber { get; set; }

    public string? InitiativeTypeCode { get; set; }

    public string? InitiativeTypeDescription { get; set; }

    public string? InitiativeSelection { get; set; }

    public string? InitiativeObservations { get; set; }

    public string? InitiativeTextSubstitution { get; set; }

    public string? InitiativeTextSubstitutionField { get; set; }

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

    public List<ParliamentInitiativeAuthor> ImportedAuthors { get; set; } = [];

    public List<ParliamentInitiativeEvent> ImportedEvents { get; set; } = [];

    public List<ParliamentInitiativeVote> ImportedVotes { get; set; } = [];

    public List<ParliamentInitiativeDocument> ImportedDocuments { get; set; } = [];

    public List<ParliamentInitiativePublication> ImportedPublications { get; set; } = [];

    public List<ParliamentInitiativeIntervention> ImportedInterventions { get; set; } = [];

    public List<ParliamentSummary> Summaries { get; set; } = [];
}
