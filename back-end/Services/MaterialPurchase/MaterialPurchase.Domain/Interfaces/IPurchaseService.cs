using Commom.Domain.Exceptions;
using Commom.Domain.SeedWork;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Enum;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Interfaces
{
    public interface IPurchaseService
    {
        Task<PurchaseResponse> AuthorizePurchase(Context context, PurchaseRequest req);
        Task<PurchaseResponse> UnlockPurchase(Context context, PurchaseRequest req);

        Task<PurchaseResponse> ConfirmDeliveryDate(Context context, ConfirmDeliveryDateRequest req);

        Task<PurchaseResponse> ReceiveDelivery(Context context, ReceiveDeliveryRequest req);

        Task<PurchaseResponse> CancelPurchaseCreator(Context context, CancelPurchaseRequest req);

        Task<PurchaseResponse> CancelPurchaseClient(Context context, CancelPurchaseRequest req);

        Task<PurchaseResponse> CancelPurchaseAdmin(Context context, CancelPurchaseRequest req);
        void CheckPurchaseAuthorizations(Purchase purchase);
    }
}
