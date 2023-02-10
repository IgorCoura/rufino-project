using Commom.Domain.BaseEntities;

namespace MaterialPurchase.Domain.Entities
{
    public class FunctionId : Entity
    {
        public string Name { get; set; } = string.Empty;
        public IEnumerable<Role> Roles { get; set; } = new List<Role>();
    }
}

