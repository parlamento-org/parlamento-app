using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Parlamento.Domain.Entities;

public class ParliamentImportError
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ParliamentImportRunId { get; set; }

    [JsonIgnore]
    public ParliamentImportRun? ParliamentImportRun { get; set; }

    public string? SourceId { get; set; }

    [Required]
    public string ErrorType { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;
}
