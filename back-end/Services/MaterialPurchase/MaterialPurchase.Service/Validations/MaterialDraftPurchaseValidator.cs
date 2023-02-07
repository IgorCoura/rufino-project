using Commom.Domain.Errors;
using Commom.Domain.Utils;
using FluentValidation;
using MaterialPurchase.Domain.Models.Request;

namespace MaterialPurchase.Service.Validations
{
    public class MaterialDraftPurchaseValidator : AbstractValidator<MaterialDraftPurchaseRequest>
    {
        public MaterialDraftPurchaseValidator() 
        {
            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0)
                .WithErrorMessage(CommomErrors.GreaterThanOrEqualValidator, x => nameof(x.UnitPrice), x => "0");

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0)
                .WithErrorMessage(CommomErrors.GreaterThanOrEqualValidator, x => nameof(x.Quantity), x => "0");
        }
    }
}
