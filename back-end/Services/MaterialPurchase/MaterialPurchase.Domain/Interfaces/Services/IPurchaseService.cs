using Commom.Domain.BaseEntities;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;

namespace MaterialPurchase.Domain.Interfaces.Services
{
    public interface IPurchaseService
    {
        Task<PurchaseResponse> AuthorizePurchase(Context context, ReleasePurchaseRequest req);
        Task<PurchaseResponse> UnlockPurchase(Context context, ReleasePurchaseRequest req);

        Task<PurchaseResponse> ConfirmDeliveryDate(Context context, ConfirmDeliveryDateRequest req);

        Task<PurchaseResponse> ReceiveDelivery(Context context, ReceiveDeliveryRequest req);

        Task<PurchaseResponse> CancelPurchaseBeforeAuthorize(Context context, CancelPurchaseRequest req);

        Task<PurchaseResponse> CancelPurchaseAdmin(Context context, CancelPurchaseRequest req);
    }
}
