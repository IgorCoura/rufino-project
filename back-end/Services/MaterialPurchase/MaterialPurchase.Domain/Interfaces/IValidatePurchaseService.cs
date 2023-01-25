using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Interfaces
{
    public interface IValidatePurchaseService
    {
        void ValidatePurchaseOrder(Guid PurchaseId);
    }
}
