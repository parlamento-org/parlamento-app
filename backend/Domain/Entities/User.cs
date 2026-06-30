using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
    [JsonIgnore]
    public string? Password { get; set; }

    [Required]
    public int ProfilePic { get; set; }

    public List<Vote> Votes { get; set; } = new();

    public List<PartyStats> PartyStats { get; set; } = new();

    [JsonIgnore]
    public string? googleIDToken { get; set; }

    [JsonIgnore]
    public string? facebookIDToken { get; set; }
}
