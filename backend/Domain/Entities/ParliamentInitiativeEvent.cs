using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class ParliamentInitiativeEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectLawId { get; set; }

    public ProjectLaw? ProjectLaw { get; set; }

    public string? SourceEventId { get; set; }

    public string? SourceActivityId { get; set; }

    public string? SourceObjectEventId { get; set; }

    public string? SourceTextId { get; set; }

    public string? PhaseCode { get; set; }

    public string? PhaseName { get; set; }

    public string? PhaseDate { get; set; }

    public string? Observation { get; set; }

    public string? ApprovedTextId { get; set; }

    public string? RawJson { get; set; }

    public List<ParliamentInitiativeVote> Votes { get; set; } = [];

    public List<ParliamentInitiativeDocument> Documents { get; set; } = [];

    public List<ParliamentInitiativePublication> Publications { get; set; } = [];

    public List<ParliamentInitiativeIntervention> Interventions { get; set; } = [];
}
