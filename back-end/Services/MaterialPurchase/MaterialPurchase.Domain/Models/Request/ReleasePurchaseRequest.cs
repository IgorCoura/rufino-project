using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Request
{
    public record ReleasePurchaseRequest : ModelBase
    {
        public ReleasePurchaseRequest(Guid id, Guid constructionId, bool approve, string comment) : base(constructionId)
        {
            Id = id;
            Approve = approve;
            Comment = comment;
        }

        public Guid Id { get; set; }
        public bool Approve { get; set; }
        public string Comment { get; set; }
    }
}
