using BuildManagement.Domain.Models.Purchase.MaterialReceive;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Validations.MaterialPurchase
{
    public class MaterialReceiveValidator : AbstractValidator<MaterialReceiveRequest>
    {
        public MaterialReceiveValidator()
        {
            RuleForEach(x => x.MaterialReceive)
                .ChildRules(child =>
                {
                    child.RuleFor(x => x.QuantityReceived)
                    .GreaterThan(0);
                });
        }
    }
}
