using Commom.Domain.Errors;
using Commom.Domain.Utils;
using FluentValidation;
using MaterialControl.Domain.Models.Request;

namespace MaterialControl.Services.Validations
{
    public class MaterialValidator : AbstractValidator<MaterialRequest>
    {
        public MaterialValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorMessage(CommomErrors.NotEmptyValidator, x => nameof(x))
                .MaximumLength(100)
                .WithErrorMessage(CommomErrors.MaximumLengthValidator, x => nameof(x), x => "100"); ;

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithErrorMessage(CommomErrors.NotEmptyValidator, x => nameof(x))
                .MaximumLength(250)
                .WithErrorMessage(CommomErrors.MaximumLengthValidator, x => nameof(x), x => "250"); ;
        }
    }
}
