using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;

namespace MaterialPurchase.Tests.Utils
{
    public static class EntitiesExtension
    {
        public static bool EqualExtesion(this CreateDraftPurchaseRequest req, CompletePurchaseResponse rep)
        {
            var repMaterial = rep.Materials.ToArray();
            var checkMaterial = req.Materials.Select((m, index) => m.EqualExtesion(repMaterial[index])).Any(x => x == false);
            return req.ProviderId == rep.Provider.Id && req.ConstructionId == rep.Construction.Id && !checkMaterial;
        }

        public static bool EqualExtesion(this DraftPurchaseRequest req, CompletePurchaseResponse rep)
        {
            var repMaterial = rep.Materials.ToArray();
            var checkMaterial = req.Materials.Select((m, index) => m.EqualExtesion(repMaterial[index])).Any(x => x == false);
            return req.Id == rep.Id && req.ProviderId == rep.Provider.Id && req.ConstructionId == rep.Construction.Id && !checkMaterial;
        }

        public static bool EqualExtesion(this CreateMaterialDraftPurchaseRequest req, ItemMaterialPurchaseResponse rep)
            => req.MaterialId == rep.Material!.Id && req.BrandId == rep.Brand!.Id && req.Quantity == rep.Quantity && req.UnitPrice == rep.UnitPrice;

        public static bool EqualExtesion(this MaterialDraftPurchaseRequest req, ItemMaterialPurchaseResponse rep)
            => req.Id == rep.Id  && req.MaterialId == rep.Material!.Id && req.BrandId == rep.Brand!.Id && req.Quantity == rep.Quantity && req.UnitPrice == rep.UnitPrice;
    }
}
