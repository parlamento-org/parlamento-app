using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Parlamento.Domain.Entities;

public class ProjectLawInteractionStats
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ProjectLawId { get; set; }

    [JsonIgnore]
    public ProjectLaw? ProjectLaw { get; set; }

    public int Impressions { get; set; }

    public int SupportVotes { get; set; }

    public int OpposeVotes { get; set; }

    public int AbstainVotes { get; set; }

    public int Skips { get; set; }

    public int DetailOpens { get; set; }

    public int SourceLinkClicks { get; set; }

    public int DebateLinkClicks { get; set; }

    public int PostVoteReveals { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
