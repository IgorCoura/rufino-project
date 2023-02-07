using Commom.Infra.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Commom.API.AuthorizationIds
{
    public class AuthorizationIdHandler : AuthorizationHandler<AuthorizationIdRequirement>
    {
        private readonly AuthorizationIdOptions _options;
        private readonly IRoleRepository _roleRepository;

        public AuthorizationIdHandler(IOptions<AuthorizationIdOptions> options, IRoleRepository roleRepository)
        {
            _options = options.Value;
            _roleRepository = roleRepository;
        }


        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizationIdRequirement requirement)
        {
            var roleName = context.User.FindFirst(r => r.Type == ClaimTypes.Role)?.Value.ToString();            

            if (roleName == null)
            {
                context.Fail();
                return;
            }

            var role = await _roleRepository.FirstAsync(x => x.Name.Equals(roleName), include: i => i.Include(o => o.FunctionsIds));

            if (role == null)
            {
                context.Fail();
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

            context.Fail();
            return;

        }
    }
}
