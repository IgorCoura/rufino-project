namespace CustSuprManager.API.Configurations
{
    public static class DependencyInjectionConfig
    {
        public static IServiceCollection AddDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            #region Repositories



            #endregion

            #region Services


            #endregion

            #region Options



            #endregion

            #region Validators

            //service.AddValidatorsFromAssemblyContaining(typeof(CreateBrandValidator));

            #endregion

            //service.AddAutoMapper(typeof(EntityToModelProfile), typeof(ModelToEntityProfile), typeof(EntityToMessageProfile));

            return service;
        }
    }
}
