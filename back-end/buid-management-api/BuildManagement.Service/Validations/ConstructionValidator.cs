using BuildManagement.Domain.Models.Construction;
using BuildManagement.Domain.Models.Material;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Validations
{
    public class CreateConstructionValidator : AbstractValidator<CreateConstructionModel>
    {
        public CreateConstructionValidator()
        {
            RuleFor(x => x.NickName)
                .MinimumLength(3)
                .MaximumLength(100);

            RuleFor(x => x.CorporateName)
                .MinimumLength(3)
                .MaximumLength(200);

            RuleFor(x => x.Street)
                .MaximumLength(100);

            RuleFor(x => x.City)
                .MaximumLength(50);

            RuleFor(x => x.State)
                .MaximumLength(50);

            RuleFor(x => x.Country)
                .MaximumLength(50);

            RuleFor(x => x.ZipCode)
                .MaximumLength(16);

        }
    }
}
