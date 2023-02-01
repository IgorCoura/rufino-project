using Microsoft.AspNetCore.Authorization;
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

        public FunctionIdHandler(IOptions<FunctionIdAuthorizationOptions> options)
        {
            _options = options.Value;
        }


        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FunctionIdRequirement requirement)
        {
            var functionIdRole = context.User.FindFirst(r => r.Type == ClaimTypes.Role);

            if (functionIdRole == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if ( requirement.Id.Equals(functionIdRole.Value.ToString()))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            context.Fail();
            return Task.CompletedTask;

        }
    }
}
