using Commom.Domain.BaseEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.BaseEntities
{
    public class Construction : Entity
    {
        public string CorporateName { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;
        public Address? Address { get; set; }
        public IEnumerable<ConstructionAuthUserGroup> PurchasingAuthorizationUserGroups { get; set; } = new List<ConstructionAuthUserGroup>();
    }
}
