using FluentValidation;
using MaterialPurchase.Domain.Interfaces.Repositories;
using MaterialPurchase.Domain.Interfaces.Services;
using MaterialPurchase.Infra.Repository;
using MaterialPurchase.Service.Consumer;
using MaterialPurchase.Service.Mappers;
using MaterialPurchase.Service.Services;
using MaterialPurchase.Service.Validations;

namespace MaterialPurchase.API.Configurations
{
    public static class DependencyInjectionConfig
    {
        public static IServiceCollection AddDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            #region Repositories

            service.AddScoped<IConstructionRepository, ConstructionRepository>();
            service.AddScoped<IPurchaseRepository, PurchaseRepository>();
            service.AddScoped<IBrandRepository, BrandRepository>();
            service.AddScoped<IMaterialRepository, MaterialRepository>();

            #endregion

            #region Services

            service.AddScoped<IDraftPurchaseService, DraftPurchaseService>();
            service.AddScoped<IPurchaseService, PurchaseService>();
            service.AddScoped<IValidatePurchaseService, ValidatePurchaseService>();
            service.AddScoped<IRecoverPurchaseService, RecoverPurchaseService>();
            service.AddScoped<IPermissionsService, PermissionsService>();

            #endregion

            #region Consumer

            service.AddScoped<BrandConsumer>();
            service.AddScoped<MaterialConsumer>();

            #endregion


            #region Options


            #endregion

            #region Validators

            service.AddValidatorsFromAssemblyContaining(typeof(CreateDraftPurchaseValidator));

            #endregion

            service.AddAutoMapper(typeof(EntityToModelProfile), typeof(ModelToEntityProfile));

            return service;
        }
    }
}
