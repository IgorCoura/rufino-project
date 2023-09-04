using Commom.API.Controllers;
using Commom.Auth.Authorization;
using MaterialPurchase.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaterialPurchase.API.Controllers
{
    public class MainController : BaseController
    {
        protected async Task<ActionResult> VerifyAuthorize(IAuthorizationService authorization, ModelBase resource, string requirement, Func<Task<ActionResult>> func)
        {
            var authorizationResult = await authorization.AuthorizeAsync(User, resource, new AuthorizationIdRequirement(requirement));
            if (authorizationResult.Succeeded)
            {
                return await func();
            }
            else if (User.Identity!.IsAuthenticated)
            {
                return new ForbidResult();
            }
            else
            {
                return new ChallengeResult();
            }
        }
    }
}
