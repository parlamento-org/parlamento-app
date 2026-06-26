using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string? UserName { get; set; }

    [Required]
    public string? Email { get; set; }

    [Required]
    public string? Password { get; set; }

    [Required]
    public int ProfilePic { get; set; }

    public List<Vote> Votes { get; set; } = new();

    public List<PartyStats> PartyStats { get; set; } = new();

    public string? googleIDToken { get; set; }

    public string? facebookIDToken { get; set; }
}
