using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class ParliamentInitiativeVote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectLawId { get; set; }

    public ProjectLaw? ProjectLaw { get; set; }

    public int? ParliamentInitiativeEventId { get; set; }

    public ParliamentInitiativeEvent? ParliamentInitiativeEvent { get; set; }

    public string? SourceVoteId { get; set; }

    [Required]
    public string Stage { get; set; } = "Other";

    public string? VoteDate { get; set; }

    public string? Description { get; set; }

    public string? Result { get; set; }

    public string? Unanimous { get; set; }

    public string? AbsencesJson { get; set; }

    public string? Detail { get; set; }

    public string? Meeting { get; set; }

    public string? MeetingType { get; set; }

    public string? PublicationJson { get; set; }

    public string? RawJson { get; set; }

    public List<ParliamentInitiativeVoteBlock> Blocks { get; set; } = [];
}
