using FluentValidation;

namespace Parlamento.Application.Users;

public class UserLoginRequestValidator : AbstractValidator<UserLoginRequest>
{
    public UserLoginRequestValidator()
    {
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.UserName))
            .WithMessage("Either email or userName must be provided.");
    }
}
