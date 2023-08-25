using Commom.Domain.BaseEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Entities
{
    public class UsePermission : Entity
    {
        public Guid UserId { get; set; }
        public IEnumerable<FunctionIdPermission> FunctionsIds { get; set; } = new List<FunctionIdPermission>();
        public Construction? Construction { get; set; }
        public Guid ConstructionId { get; set; }
    }
}
