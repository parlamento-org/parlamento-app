using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class ParliamentInitiativePublication
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectLawId { get; set; }

    public ProjectLaw? ProjectLaw { get; set; }

    public int? ParliamentInitiativeEventId { get; set; }

    public ParliamentInitiativeEvent? ParliamentInitiativeEvent { get; set; }

    [Required]
    public string Scope { get; set; } = string.Empty;

    public string? PublicationDate { get; set; }

    public string? Legislature { get; set; }

    public string? Number { get; set; }

    public string? Series { get; set; }

    public string? Type { get; set; }

    public string? TypeCode { get; set; }

    public string? PagesJson { get; set; }

    public string? DiaryUrl { get; set; }

    public string? RawJson { get; set; }
}
