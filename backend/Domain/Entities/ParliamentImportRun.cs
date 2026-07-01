using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class ParliamentImportRun
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string SourceKind { get; set; } = string.Empty;

    public string? Legislature { get; set; }

    public string? SourceReference { get; set; }

    [Required]
    public DateTime StartedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }

    [Required]
    public string Status { get; set; } = "Running";

    public int RecordsRead { get; set; }

    public int RecordsInserted { get; set; }

    public int RecordsUpdated { get; set; }

    public int RecordsSkipped { get; set; }

    public int RecordsFailed { get; set; }

    public string? ErrorMessage { get; set; }

    public List<ParliamentImportSkip> Skips { get; set; } = [];

    public List<ParliamentImportError> Errors { get; set; } = [];
}
