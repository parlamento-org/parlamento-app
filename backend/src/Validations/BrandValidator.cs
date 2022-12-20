using backend.Models;

using FluentValidation;

namespace backend.Validations;

public class BrandValidator : AbstractValidator<Brand>
{
    private readonly DatabaseContext _db;

    private class ContactInfoValidator : AbstractValidator<ContactInfo>
    {
        public ContactInfoValidator()
        {
            RuleFor(x => x.Name).NotNull().NotEmpty();
            RuleFor(x => x.PhoneNumber).NotNull();
        }
    }
    private class SalesChannelValidator : AbstractValidator<SalesChannel>
    {
        public SalesChannelValidator()
        {
            RuleFor(x => x.Name).NotNull().IsInEnum();
            RuleFor(x => x.fullOrWait).NotNull();
            RuleFor(x => x.isActive).NotNull();
            RuleFor(x => x.partialAndCancel).NotNull();
            RuleFor(x => x.prePrintedReturnLabel).NotNull();
            RuleFor(x => x.digitalReturnLabel).NotNull();
        }
    }
    public BrandValidator(DatabaseContext db)
    {
        this._db = db;
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256).WithMessage("Brand Name is required");

        RuleFor(x => x.contactInfos).NotNull().WithMessage("Contacto Info is required");
        RuleForEach(x => x.contactInfos).SetValidator(new ContactInfoValidator());

        RuleFor(x => x.SalesChannels).NotNull().WithMessage("Sales Channels are required");
        RuleForEach(x => x.SalesChannels).SetValidator(new SalesChannelValidator());


    }


}