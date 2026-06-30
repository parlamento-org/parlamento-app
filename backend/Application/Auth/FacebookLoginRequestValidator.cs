using FluentValidation;

namespace Parlamento.Application.Auth;

public class FacebookLoginRequestValidator : AbstractValidator<FacebookLoginRequest>
{
    public FacebookLoginRequestValidator()
    {
        RuleFor(x => x.FacebookAccessToken).NotEmpty();
        RuleFor(x => x.ProfilePic).GreaterThanOrEqualTo(0);
    }
}
