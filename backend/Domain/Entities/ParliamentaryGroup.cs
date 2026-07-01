using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class ParliamentaryGroup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string Legislature { get; set; } = string.Empty;

    [Required]
    public string Acronym { get; set; } = string.Empty;

    public string? Name { get; set; }
}
