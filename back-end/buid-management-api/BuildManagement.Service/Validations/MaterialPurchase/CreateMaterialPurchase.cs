using BuildManagement.Domain.Exceptions;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Models.Purchase.CreateMaterialPurchase;
using BuildManagement.Service.Validations.Utils;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Validations.MaterialPurchase
{
    public class CreateMaterialPurchase : AbstractValidator<CreateMaterialPurchaseRequest>
    {
        public CreateMaterialPurchase(
            IProviderRepository providerRepository,
            IConstructionRepository constructionRepository,
            IBrandRepository brandRepository,
            IMaterialRepository materialRepository)
        {
            RuleFor(x => x.Freight)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.ProviderId)
                .MustAsync((entity, field, cancellationToken) => providerRepository.HasAnyAsync(x => x.Id == field))
                .WithErrorMessage(ErrorsMessages.FieldNotFound, (entity, field) => field.ToString());

            RuleFor(x => x.ConstructionId)
                .MustAsync((entity, field, cancellationToken) => constructionRepository.HasAnyAsync(x => x.Id == field))
                .WithErrorMessage(ErrorsMessages.FieldNotFound, (entity, field) => field.ToString());

            RuleForEach(x => x.Materials)
                .ChildRules(child =>
                {
                    child.RuleFor(i => i.UnitPrice)
                        .GreaterThan(0);

                    child.RuleFor(i => i.Quantity)
                        .GreaterThan(0);

                    child.RuleFor(i => i.BrandId)
                        .MustAsync((entity, field, cancellationToken) => brandRepository.HasAnyAsync(x => x.Id == field))
                        .WithErrorMessage(ErrorsMessages.FieldNotFound, (entity, field) => field.ToString()); ;

                    child.RuleFor(i => i.MaterialId)
                        .MustAsync((entity, field, cancellationToken) => materialRepository.HasAnyAsync(x => x.Id == field))
                        .WithErrorMessage(ErrorsMessages.FieldNotFound, (entity, field) => field.ToString());
                    
                });
        }
    }
}
