using FluentValidation;

namespace Parlamento.Application.Proposals;

public class CreateProposalRequestValidator : AbstractValidator<CreateProposalRequest>
{
    public CreateProposalRequestValidator()
    {
        RuleFor(x => x.SourceId).NotNull().GreaterThan(0);
        RuleFor(x => x.Legislatura).NotEmpty();
        RuleFor(x => x.ProposalTitle).NotEmpty();
        RuleFor(x => x.FullProposalTextLink).NotEmpty();
        RuleFor(x => x.ProposingPartyAcronym).NotEmpty();
    }
}
