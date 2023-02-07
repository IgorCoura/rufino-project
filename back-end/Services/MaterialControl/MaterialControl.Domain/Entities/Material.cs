using Commom.Domain.BaseEntities;

namespace MaterialControl.Domain.Entities
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

        public string Description { get; set; } = string.Empty;
        public Unity? Unity { get; set; }
        public Guid UnityId { get; set; }
    }
}
