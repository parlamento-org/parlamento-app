using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class ParliamentDeputy
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string Legislature { get; set; } = string.Empty;

    public string? SourceCadId { get; set; }

    public string? SourceDeputyId { get; set; }

    public string? FullName { get; set; }

    public string? ParliamentaryName { get; set; }

    public string? Constituency { get; set; }

    public string? PartyAcronym { get; set; }

    public string? Situation { get; set; }
}
