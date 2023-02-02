using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using Commom.Infra.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Commom.API.FunctionIdAuthorization
{
    public class FunctionIdHandler : AuthorizationHandler<FunctionIdRequirement>
    {
        private readonly FunctionIdAuthorizationOptions _options;
        private readonly IRoleRepository _roleRepository;

        public FunctionIdHandler(IOptions<FunctionIdAuthorizationOptions> options, IRoleRepository roleRepository)
        {
            _options = options.Value;
            _roleRepository = roleRepository;
        }


        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, FunctionIdRequirement requirement)
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

            if (role.FunctionsIds.Any(x => x.Id.ToString() == requirement.Id))
            {
                context.User.AddIdentity(new ClaimsIdentity(role.FunctionsIds.Select(x =>
                {
                    return new Claim("FunctionId", x.Name);
                })));

                context.Succeed(requirement);
                return;
            }

            context.Fail();
            return;

        }
    }
}
