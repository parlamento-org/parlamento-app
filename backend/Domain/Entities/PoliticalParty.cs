using System.ComponentModel.DataAnnotations;

namespace Parlamento.Domain.Entities;

public class PoliticalParty
{
    [Key]
    [Required]
    public string? partyAcronym { get; set; }

    [Required]
    public string? fullName { get; set; }

    [Required]
    public string? logoLink { get; set; }
}
