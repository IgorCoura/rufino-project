using FluentValidation;
using MaterialPurchase.Domain.Models.Request;

namespace MaterialPurchase.Service.Validations
{
    public class CreateDraftPurchaseValidator : AbstractValidator<CreateDraftPurchaseRequest>
    {
        public CreateDraftPurchaseValidator()
        {
            RuleFor(x => x.Freight)
                .GreaterThanOrEqualTo(0);

            RuleForEach(x => x.Materials)
                .SetValidator(new CreateMaterialDraftPurchaseValidator());
        }
    }
}
