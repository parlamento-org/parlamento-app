using FluentValidation;

namespace Parlamento.Application.Proposals;

public class UpdateProposalRequestValidator : AbstractValidator<UpdateProposalRequest>
{
    public UpdateProposalRequestValidator()
    {
        RuleFor(x => x.SourceId).GreaterThan(0).When(x => x.SourceId.HasValue);
        RuleFor(x => x.Score).GreaterThanOrEqualTo(0).When(x => x.Score.HasValue);
    }
}
