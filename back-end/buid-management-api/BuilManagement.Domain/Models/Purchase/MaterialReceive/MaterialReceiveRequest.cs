using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.MaterialReceive
{
    public record MaterialReceiveRequest(
        Guid MaterialPurchaseId,
        List<ItemMaterialReceiveRequest> MaterialReceive
        );
}
