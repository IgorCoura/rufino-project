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
        Task<SimplePurchaseResponse> SimpleRecover(PurchaseRequest req);

        Task<PurchaseWithMaterialResponse> RecoverPurchaseWithMaterials(PurchaseRequest req);
        Task<CompletePurchaseResponse> RecoverPurchaseComplete(PurchaseRequest req);
        Task<IEnumerable<CompletePurchaseResponse>> RecoverPurchaseAllComplete();
    }
}
