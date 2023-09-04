using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Request
{
    public record ConfirmDeliveryDateRequest : ModelBase
    {
        public ConfirmDeliveryDateRequest(Guid constructionId, Guid companyId, Guid purchaseId, DateTime limitDeliveryDate):base(constructionId, companyId)
        {
            PurchaseId = purchaseId;
            LimitDeliveryDate = limitDeliveryDate;
        }

        public Guid PurchaseId { get; set; }
        public DateTime LimitDeliveryDate { get; set; }
    }
}
