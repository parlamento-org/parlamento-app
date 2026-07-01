using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Parlamento.Domain.Entities;

public class ParliamentInitiativeAuthor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectLawId { get; set; }

    [JsonIgnore]
    public ProjectLaw? ProjectLaw { get; set; }

    [Required]
    public string AuthorKind { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? Acronym { get; set; }

    public string? DeputySourceId { get; set; }
}
