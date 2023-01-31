using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Request
{
    public record CreateMaterialDraftPurchaseRequest(
        Guid MaterialId,
        Guid BrandId,
        decimal UnitPrice,
        double Quantity
        );

}
