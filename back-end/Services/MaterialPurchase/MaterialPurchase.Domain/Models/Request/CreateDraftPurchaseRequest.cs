using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Request
{
    public record CreateDraftPurchaseRequest : ModelBase
    {
        public CreateDraftPurchaseRequest(Guid constructionId, 
            Guid providerId,
            decimal freight,
            CreateMaterialDraftPurchaseRequest[] materials):base(constructionId)
        {
            ProviderId = providerId;
            Freight = freight;
            Materials = materials;
        }

        public Guid ProviderId { get; set; }    
        public decimal Freight { get; set; }
        public CreateMaterialDraftPurchaseRequest[] Materials { get; set; } = Array.Empty<CreateMaterialDraftPurchaseRequest>();
    }

}