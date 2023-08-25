using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Request
{
    public record DraftPurchaseRequest : ModelBase
    {
        public DraftPurchaseRequest(Guid id, Guid providerId, Guid constructionId, decimal freight, MaterialDraftPurchaseRequest[] materials) : base(constructionId)
        {
            Id = id;
            ProviderId = providerId;
            Freight = freight;
            Materials = materials;
        }

        public Guid Id { get; set; }
        public Guid ProviderId { get; set; }
        public decimal Freight { get; set;}
        public MaterialDraftPurchaseRequest[] Materials { get; set; } = Array.Empty<MaterialDraftPurchaseRequest>();
    }


}

