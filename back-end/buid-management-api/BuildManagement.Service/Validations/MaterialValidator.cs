using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Models.Material;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Validations
{
    public class CreateMaterialValidator : AbstractValidator<CreateMaterialModel>
    {
        public CreateMaterialValidator(IMaterialRepository materialRepository)
        {
            RuleFor(x => x.Name)
                .MinimumLength(3)
                .MaximumLength(200)
                .MustAsync(
                    async (entity, name, context, cancellationToken) =>
                       (await materialRepository.HasAnyAsync(x => x.Name.ToUpper() == name.ToUpper()) is false)
                )
                .WithMessage(x => $"Brand name {x.Name} already existe"); ;

            RuleFor(x => x.Description)
                .MinimumLength(10)
                .MaximumLength(500);

            RuleFor(x => x.Unity)
                .MinimumLength(1)
                .MaximumLength(50);

        }
    }
}
