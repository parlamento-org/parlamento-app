using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class PartyStats
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public PoliticalParty? PoliticalParty { get; set; }

    [Required]
    public double PartyAffectionScore { get; set; }

    [Required]
    public int totalAmountOfProposalsVotedOn { get; set; }

    [Required]
    public double totalAffectionPoints { get; set; }
}
