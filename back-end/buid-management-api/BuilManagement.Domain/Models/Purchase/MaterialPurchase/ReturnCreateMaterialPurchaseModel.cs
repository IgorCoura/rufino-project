using BuildManagement.Domain.Models.Construction;
using BuildManagement.Domain.Models.Purchase.ItemMaterialPurchase;
using BuildManagement.Domain.Models.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.MaterialPurchase
{
    public record ReturnCreateMaterialPurchaseModel
    (
        Guid Id,
        DateTime CreateAt
    );
}
