using Commom.Infra.Interface;
using Commom.Infra.Repository;
using FluentValidation;
using MaterialPurchase.Domain.Interfaces;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Infra.Context;
using MaterialPurchase.Infra.Repository;
using MaterialPurchase.Service.Mappers;
using MaterialPurchase.Service.Services;
using MaterialPurchase.Service.Validations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace MaterialPurchase.API.Configurations
{
    public static class DependencyInjectionConfig
    {
        public static IServiceCollection AddDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            #region Repositories

            service.AddScoped<IConstructionRepository, ConstructionRepository>();
            service.AddScoped<IPurchaseRepository, PurchaseRepository>();
            

            #endregion

            #region Services

            service.AddScoped<IDraftPurchaseService, DraftPurchaseService>();
            service.AddScoped<IPurchaseService, PurchaseService>();
            service.AddScoped<IValidatePurchaseService, ValidatePurchaseService>();
            service.AddScoped<IRecoverPurchaseService, RecoverPurchaseService>();
            service.AddScoped<IPermissionsService, PermissionsService>();

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
