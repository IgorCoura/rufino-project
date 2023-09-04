using Commom.Auth.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Commom.Auth.Authorization
{
    public class AuthorizationIdHandler : AuthorizationHandler<AuthorizationIdRequirement>
    {
        private readonly DbContext _context;

        public AuthorizationIdHandler(DbContext context)
        {
            _context = context;
        }


        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizationIdRequirement requirement)
        {

            var roleName = context.User.FindFirst(r => r.Type == ClaimTypes.Role)?.Value.ToString();

            if (roleName == null)
            {
                return;
            }

            var role = await _context
                .Set<Role>()
                .Include(x => x.FunctionsIds)
                .FirstOrDefaultAsync(x => x.Name.Equals(roleName));

            if (role == null)
            {          
                return;
            }

            if (role.FunctionsIds.Any(x => x.Name == requirement.Id))
            {
                context.User.AddIdentity(new ClaimsIdentity(role.FunctionsIds.Select(x =>
                {
                    return new Claim(AuthorizationIdOptions.POLICY_PREFIX, x.Name);
                })));

                context.Succeed(requirement);
                return;
            }

            return;

        }
    }
}
