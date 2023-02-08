using Commom.Domain.Errors;
using Commom.Domain.Utils;
using FluentValidation;
using MaterialControl.Domain.Models.Request;

namespace MaterialControl.Services.Validations
{
    public class BrandValidator : AbstractValidator<BrandRequest>
    {
        public BrandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorMessage(CommomErrors.NotEmptyValidator, x => nameof(x))
                .MaximumLength(50)
                .WithErrorMessage(CommomErrors.MaximumLengthValidator, x => nameof(x), x => "50");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithErrorMessage(CommomErrors.NotEmptyValidator, x => nameof(x))
                .MaximumLength(250)
                .WithErrorMessage(CommomErrors.MaximumLengthValidator, x => nameof(x), x => "250");
        }
    }
}
