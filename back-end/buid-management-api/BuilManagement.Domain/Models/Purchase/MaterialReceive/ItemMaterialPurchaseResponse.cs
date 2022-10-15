using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.MaterialReceive
{
    public record ItemMaterialPurchaseResponse(
            Guid Id,
            int Quantity,
            int QuantityNotReceived,
            string Status
        );
}

