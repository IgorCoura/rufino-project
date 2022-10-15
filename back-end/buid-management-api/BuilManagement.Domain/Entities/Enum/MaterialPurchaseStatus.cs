using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities.Enum
{
    public enum MaterialPurchaseStatus
    {
        Pending,
        Reproved,
        PreAuthorized,
        Authorized,
        PartialReceived,
        Received
    }
}
