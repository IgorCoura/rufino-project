using Commom.Domain.BaseEntities;

namespace Identity.API.Application.Entities
{
    public class UserRefreshToken : Entity
    {
        public Guid UserId { get; set; }
        public DateTime ExpireDate { get; set; }
    }
}
