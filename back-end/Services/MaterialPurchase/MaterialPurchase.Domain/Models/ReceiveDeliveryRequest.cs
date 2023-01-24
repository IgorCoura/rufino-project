using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models
{
    public record ReceiveDeliveryRequest
    (
        Guid PurchaseId,
        DateTime DeliveryDate,
        ReceiveDeliveryItemRequest[] ReceiveDeliveryItemRequests
        
    );

    public record ReceiveDeliveryItemRequest
    (
        Guid MaterialPurchaseId,
        double Quantity
    );
}
