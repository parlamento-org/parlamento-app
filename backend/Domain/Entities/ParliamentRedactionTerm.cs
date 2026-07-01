using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class ParliamentRedactionTerm
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string Legislature { get; set; } = string.Empty;

    [Required]
    public string Term { get; set; } = string.Empty;

    [Required]
    public string TermKind { get; set; } = string.Empty;
}
