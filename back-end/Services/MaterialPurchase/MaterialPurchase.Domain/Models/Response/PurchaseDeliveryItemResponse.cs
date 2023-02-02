using MaterialPurchase.Domain.BaseEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Response
{
    public record PurchaseDeliveryItemResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        Guid MaterialPurchaseId,
        double Quantity,
        UserResponse? Receiver
    );
}
