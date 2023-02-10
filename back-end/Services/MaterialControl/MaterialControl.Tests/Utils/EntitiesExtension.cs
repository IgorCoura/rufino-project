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

        public static bool EqualExtesion(this CreateUnityRequest request, UnityResponse response)
            => request.Name.ToUpper() == response.Name;
        public static bool EqualExtesion(this UnityRequest request, UnityResponse response)
            => request.Id == response.Id && request.Name.ToUpper() == response.Name;
        public static bool EqualExtesion(this CreateMaterialRequest request, MaterialResponse response)
            => request.Name.ToUpper() == response.Name.ToUpper() && request.Description.ToUpper() == response.Description.ToUpper();
        public static bool EqualExtesion(this MaterialRequest request, MaterialResponse response)
            => request.Id == response.Id && request.Name.ToUpper() == response.Name.ToUpper() && request.Description.ToUpper() == response.Description.ToUpper();
    }
}
