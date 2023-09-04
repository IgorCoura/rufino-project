using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models
{
    public record ModelBase
    {
        public Guid ConstructionId { get; set; }
        public Guid CompanyId { get; set; }

        public ModelBase(Guid constructionId, Guid companyId)
        {
            ConstructionId = constructionId;
            CompanyId = companyId;
        }
    }
}

