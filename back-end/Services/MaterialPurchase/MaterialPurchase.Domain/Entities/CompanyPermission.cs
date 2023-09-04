using Commom.Domain.BaseEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Entities
{
    public class CompanyPermission : Entity
    {
        public Construction? Construction { get; set; }
        public Guid ConstructionId { get; set; }
        public Company? Company { get; set; }
        public Guid CompanyId { get; set; }
        public IEnumerable<UsePermission> UsePermissions { get; set; } = new List<UsePermission>();
    }
}
