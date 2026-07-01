using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class ParliamentInitiativeIntervention
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectLawId { get; set; }

    public ProjectLaw? ProjectLaw { get; set; }

    public int? ParliamentInitiativeEventId { get; set; }

    public ParliamentInitiativeEvent? ParliamentInitiativeEvent { get; set; }

    public string? PlenaryMeetingDate { get; set; }

    public string? SpeakerName { get; set; }

    public string? SpeakerParty { get; set; }

    public string? GovernmentMemberName { get; set; }

    public string? GovernmentMemberRole { get; set; }

    public string? StartTime { get; set; }

    public string? EndTime { get; set; }

    public string? Summary { get; set; }

    public string? VideoLinksJson { get; set; }

    public string? PublicationsJson { get; set; }

    public string? RawJson { get; set; }
}
