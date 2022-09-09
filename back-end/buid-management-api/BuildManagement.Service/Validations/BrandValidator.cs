using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Models.Brand;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Validations
{
    public class CreateBrandValidator : AbstractValidator<CreateBrandModel>
    {

        public CreateBrandValidator(IBrandRepository brandRepository)
        {
            RuleFor(x => x.Name)
                .MinimumLength(3)
                .MaximumLength(50)
                .MustAsync(
                    async (brand, brandName, context, cancellationToken) =>
                       (await brandRepository.HasAnyAsync(x => x.Name.ToUpper() == brandName.ToUpper()) is false)
                )
                .WithMessage(x => $"Brand name {x.Name} already existe");

            RuleFor(x => x.Description)
                .MinimumLength(10)
                .MaximumLength(500);
        }

    }
}
