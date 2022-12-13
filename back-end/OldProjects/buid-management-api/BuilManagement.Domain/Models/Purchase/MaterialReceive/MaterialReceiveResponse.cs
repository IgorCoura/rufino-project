using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.MaterialReceive
{
    public record MaterialReceiveResponse(
            Guid Id,
            List<ItemMaterialPurchaseResponse> Materials,
            String Status
        );
}
