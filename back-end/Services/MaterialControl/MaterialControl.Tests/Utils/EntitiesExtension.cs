using MaterialControl.Domain.Models.Request;
using MaterialControl.Domain.Models.Response;

namespace MaterialControl.Tests.Utils
{
    public static class EntitiesExtension
    {
        public static bool EqualExtesion(this CreateBrandRequest request, BrandResponse response)
            => request.Name.ToUpper() == response.Name && request.Description == response.Description;

        public static bool EqualExtesion(this BrandRequest request, BrandResponse response)
            => request.Id == response.Id && request.Name.ToUpper() == response.Name && request.Description == response.Description;
    }
}
