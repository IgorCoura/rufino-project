using Commom.Domain.BaseEntities;

namespace MaterialControl.Domain.Entities
{
    public class User : Entity
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}

