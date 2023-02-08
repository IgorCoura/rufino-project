using Commom.Domain.Errors;
using Commom.Domain.Utils;
using FluentValidation;
using MaterialControl.Domain.Models.Request;

namespace MaterialControl.Services.Validations
{
    public class CreateUnityValidator : AbstractValidator<CreateUnityRequest>
    {
        public CreateUnityValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorMessage(CommomErrors.NotEmptyValidator, x => nameof(x))
                .MaximumLength(25)
                .WithErrorMessage(CommomErrors.MaximumLengthValidator, x => nameof(x), x => "25"); ;
        }
    }
}
