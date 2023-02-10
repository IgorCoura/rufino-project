using Commom.Domain.BaseEntities;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;

namespace MaterialPurchase.Domain.Interfaces.Services
{
    public interface IPurchaseService
    {
        Task<PurchaseResponse> AuthorizePurchase(Context context, PurchaseRequest req);
        Task<PurchaseResponse> UnlockPurchase(Context context, PurchaseRequest req);

        Task<PurchaseResponse> ConfirmDeliveryDate(Context context, ConfirmDeliveryDateRequest req);

        Task<PurchaseResponse> ReceiveDelivery(Context context, ReceiveDeliveryRequest req);

        Task<PurchaseResponse> CancelPurchaseBeforeAuthorize(Context context, CancelPurchaseRequest req);
        Task<PurchaseResponse> CancelPurchaseDuringAuthorize(Context context, CancelPurchaseRequest req);
        Task<PurchaseResponse> CancelPurchaseAfterAuthorize(Context context, CancelPurchaseRequest req);
    }
}
