using Commom.Domain.BaseEntities;

namespace MaterialPurchase.Domain.Entities
{
    public class Material : Entity
    {
        private string _name = string.Empty;

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value.ToUpper();
            }
        }
        public string Unity { get; set; } = string.Empty;
    }
}
