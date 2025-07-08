using Microsoft.Extensions.Options;

namespace PeopleManagement.API.Authorization
{
    public static class AccessTokenPropagationExtensions
    {
        public static IHttpClientBuilder AddHeaderPropagation(this IHttpClientBuilder builder) =>
        builder.AddHttpMessageHandler(
            (sp) =>
            {
                var contextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var options = sp.GetRequiredService<AuthorizationOptions>();

                return new AccessTokenPropagationHandler(contextAccessor, options);
            }
        );
    }
}
