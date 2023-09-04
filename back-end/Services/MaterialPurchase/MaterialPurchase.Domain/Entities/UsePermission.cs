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
        public User? User { get; set; }
        public Guid UserId { get; set; }
        public CompanyPermission? CompanyPermission { get; set; }
        public Guid CompanyPermissionId { get; set; }
        public IEnumerable<FunctionIdPermission> FunctionsIds { get; set; } = new List<FunctionIdPermission>();
        
    }
}
