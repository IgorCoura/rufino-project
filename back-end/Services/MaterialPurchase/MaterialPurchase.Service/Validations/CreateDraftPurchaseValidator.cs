using FluentValidation;
using MaterialPurchase.Domain.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Service.Validations
{
    public class CreateDraftPurchaseValidator : AbstractValidator<CreateDraftPurchaseRequest>
    {
        public CreateDraftPurchaseValidator()
        {
            
        }
    }
}
