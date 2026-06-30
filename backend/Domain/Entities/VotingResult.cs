using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parlamento.Domain.Entities;

public class VotingResult
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public bool isUninamous { get; set; }

    public List<VotingBlock>? votingBlocks { get; set; }
}
