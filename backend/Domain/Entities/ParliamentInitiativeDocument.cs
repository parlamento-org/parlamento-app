using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Parlamento.Domain.Entities;

public class ParliamentInitiativeDocument
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectLawId { get; set; }

    [JsonIgnore]
    public ProjectLaw? ProjectLaw { get; set; }

    public int? ParliamentInitiativeEventId { get; set; }

    [JsonIgnore]
    public ParliamentInitiativeEvent? ParliamentInitiativeEvent { get; set; }

    [Required]
    public string Scope { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? DocumentType { get; set; }

    public string? DocumentDate { get; set; }

    public string? Url { get; set; }

    public string? CommitteeId { get; set; }

    public string? CommitteeName { get; set; }
}
