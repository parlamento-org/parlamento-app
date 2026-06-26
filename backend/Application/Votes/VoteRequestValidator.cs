using FluentValidation;

namespace Parlamento.Application.Votes;

public class VoteRequestValidator : AbstractValidator<VoteRequest>
{
    public VoteRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.ProjectLawId).GreaterThan(0);
        RuleFor(x => x.VotingOrientation).IsInEnum();
    }
}
