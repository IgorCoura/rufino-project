using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Request
{
    public record ReceiveDeliveryRequest : ModelBase
    {
        public ReceiveDeliveryRequest(Guid constructionId, Guid purchaseId, DateTime deliveryDate, ReceiveDeliveryItemRequest[] receiveDeliveryItemRequests) : base(constructionId)
        {
            PurchaseId = purchaseId;
            DeliveryDate = deliveryDate;
            ReceiveDeliveryItemRequests = receiveDeliveryItemRequests;
        }

        public Guid PurchaseId { get; set; }
        public DateTime DeliveryDate { get; set; }
        public ReceiveDeliveryItemRequest[] ReceiveDeliveryItemRequests { get; set; } = Array.Empty<ReceiveDeliveryItemRequest>();
    }
    

    public record ReceiveDeliveryItemRequest
    (
        Guid MaterialPurchaseId,
        double Quantity
    );
}
