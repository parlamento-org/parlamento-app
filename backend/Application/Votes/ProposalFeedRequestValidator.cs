using FluentValidation;

namespace Parlamento.Application.Votes;

public class ProposalFeedRequestValidator : AbstractValidator<ProposalFeedRequest>
{
    public ProposalFeedRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.LowestScoreAllowed).GreaterThanOrEqualTo(0);
    }
}
