using static PeopleManagement.API.Authorization.AuthorizationServerClient;

namespace PeopleManagement.API.Authorization
{
    public interface IAuthorizationServerClient
    {
        Task<bool> VerifyAccessToResouce(string permission, CancellationToken cancellationToken = default);
    }
}
