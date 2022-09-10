using BuildManagement.Domain.Models.Provider;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Validations
{
    public class CreateProviderValidator : AbstractValidator<CreateProviderModel>
    {
        public CreateProviderValidator()
        {
            RuleFor(x => x.Name)
                .MinimumLength(3)
                .MaximumLength(200);

            RuleFor(x => x.Description)
                .MinimumLength(10)
                .MaximumLength(500);

            RuleFor(x => x.Cnpj)
                .MinimumLength(8)
                .MaximumLength(18);

            RuleFor(x => x.Email)
                .MaximumLength(100)
                .EmailAddress();

            RuleFor(x => x.Site)
                .MaximumLength(100);

            RuleFor(x => x.Phone)
                .MaximumLength(20);

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
