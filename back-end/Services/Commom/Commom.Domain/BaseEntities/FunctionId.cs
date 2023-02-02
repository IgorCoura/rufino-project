using Commom.Domain.BaseEntities;

namespace MaterialPurchase.Domain.BaseEntities
{
    public class FunctionId : Entity
    {
        public string Name { get; set; } = string.Empty;
        public IEnumerable<Role> Roles { get; set; } = Enumerable.Empty<Role>();
    }
}

