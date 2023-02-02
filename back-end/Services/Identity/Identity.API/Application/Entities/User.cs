using Commom.Domain.BaseEntities;

namespace Identity.API.Application.Entities
{
    public class User : Entity
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
