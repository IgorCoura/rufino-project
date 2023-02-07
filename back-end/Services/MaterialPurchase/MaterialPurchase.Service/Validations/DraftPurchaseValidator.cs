using Commom.Domain.Errors;
using Commom.Domain.Utils;
using FluentValidation;
using MaterialPurchase.Domain.Models.Request;

namespace MaterialPurchase.Service.Validations
{
    public class DraftPurchaseValidator : AbstractValidator<DraftPurchaseRequest>
    {
        public DraftPurchaseValidator() 
        { 
            RuleFor(x => x.Freight)
                .GreaterThanOrEqualTo(0)
                .WithErrorMessage(CommomErrors.GreaterThanOrEqualValidator, x => nameof(x.Freight), x => "0");

            RuleForEach(x => x.Materials)
                .SetValidator(new MaterialDraftPurchaseValidator());
        }
    }
}
