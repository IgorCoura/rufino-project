using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Enum
{
    public enum PurchaseStatus
    {
        Open,
        Pending,
        Blocked,
        Authorizing,
        Cancelled,
        Approved,
        WaitingDelivery,
        Closed
    }
}
