using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Request
{
    public record CancelPurchaseRequest : ModelBase
    {
        public CancelPurchaseRequest(Guid constructioId, Guid purchaseId, string comment) : base(constructioId)
        {
            PurchaseId = purchaseId;
            Comment = comment;
        }

        public Guid PurchaseId { get; set; }
        public string Comment { get; set; }
    }
}
