using Commom.Domain.Exceptions;
using Commom.Domain.SeedWork;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Enum;
using MaterialPurchase.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Interfaces
{
    public interface IPurchaseService
    {
        Task AuthorizePurchase(Context context, Guid purchaseId);
        Task UnlockPurchase(Context context, Guid purchaseId);

        Task ConfirmDeliveryDate(Context context, ConfirmDeliveryDateRequest req);

        Task ReceiveDelivery(Context context, ReceiveDeliveryRequest req);

        Task CancelPurchaseCreator(Context context, CancelPurchaseRequest req);

        Task CancelPurchaseClient(Context context, CancelPurchaseRequest req);

        Task CancelPurchaseAdmin(Context context, CancelPurchaseRequest req);
        void CheckPurchaseAuthorizations(Purchase purchase);
    }
}
