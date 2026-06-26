using FluentValidation;

namespace Parlamento.Application.Auth;

public class FacebookLoginRequestValidator : AbstractValidator<FacebookLoginRequest>
{
    public FacebookLoginRequestValidator()
    {
        RuleFor(x => x.FacebookIdToken).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.UserName).NotEmpty();
        RuleFor(x => x.ProfilePic).GreaterThanOrEqualTo(0);
    }
}
