using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Parlamento.Domain.Entities;

public class ParliamentImportSkip
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ParliamentImportRunId { get; set; }

    [JsonIgnore]
    public ParliamentImportRun? ParliamentImportRun { get; set; }

    public string? SourceId { get; set; }

    public string? InitiativeTypeCode { get; set; }

    public string? InitiativeTypeDescription { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    public string? Message { get; set; }
}
