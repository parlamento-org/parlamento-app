using FluentValidation;

namespace Parlamento.Application.Auth;

public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.GoogleIdToken).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.UserName).NotEmpty();
        RuleFor(x => x.ProfilePic).GreaterThanOrEqualTo(0);
    }
}
