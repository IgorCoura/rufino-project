﻿using FluentValidation;
using MaterialPurchase.Domain.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Service.Validations
{
    public class CreateMaterialDraftPurchaseValidator : AbstractValidator<CreateMaterialDraftPurchaseRequest>
    {
        public CreateMaterialDraftPurchaseValidator()
        {
            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0);
        }
    }
}