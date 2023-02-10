using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Interfaces.Services
{
    public interface IValidatePurchaseService
    {
        void ValidatePurchaseOrder(Guid PurchaseId);
    }
}
