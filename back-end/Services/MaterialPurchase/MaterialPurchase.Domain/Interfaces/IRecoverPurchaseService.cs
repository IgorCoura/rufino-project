using Commom.Domain.Exceptions;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Interfaces
{
    public interface IRecoverPurchaseService
    {
        Task<SimplePurchaseResponse> SimpleRecover(Guid id);
        Task<PurchaseWithMaterialResponse> RecoverPurchaseWithMaterials(Guid id);
        Task<CompletePurchaseResponse> RecoverPurchaseComplete(Guid id);
        Task<IEnumerable<CompletePurchaseResponse>> RecoverAllPurchaseComplete();
        Task<IEnumerable<PurchaseWithMaterialResponse>> RecoverAllPurchaseWithMaterials();
        Task<IEnumerable<SimplePurchaseResponse>> SimpleRecoverAll();
    }
}
