using Microsoft.AspNetCore.Authorization;

namespace Commom.API.AuthorizationIds
{
    public class AuthorizationIdRequirement : IAuthorizationRequirement
    {
        public AuthorizationIdRequirement (string id) =>
        Id =  id;

        public string Id  { get; }
    }
}
