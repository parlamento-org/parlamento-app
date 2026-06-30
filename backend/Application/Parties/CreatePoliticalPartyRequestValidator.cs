using FluentValidation;

namespace Parlamento.Application.Parties;

public class CreatePoliticalPartyRequestValidator : AbstractValidator<CreatePoliticalPartyRequest>
{
    public CreatePoliticalPartyRequestValidator()
    {
        RuleFor(x => x.PartyAcronym).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.LogoLink).NotEmpty();
    }
}
