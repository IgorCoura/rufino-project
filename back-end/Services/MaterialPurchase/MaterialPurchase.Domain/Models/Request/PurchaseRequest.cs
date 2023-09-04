using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Request
{
    public record PurchaseRequest : ModelBase
    {
        public PurchaseRequest(Guid id, Guid constructionId, Guid companyId): base(constructionId, companyId)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}
