using MaterialPurchase.Domain.BaseEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Domain.BaseEntities
{
    public class Role : Entity
    {
        public string Name { get; set; } = string.Empty;
        public IEnumerable<FunctionId> FunctionsIds { get; set; } = new List<FunctionId>();
    }
}
