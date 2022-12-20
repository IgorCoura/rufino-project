using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _config;

        public FunctionIdHandler(IConfiguration config)
        {
            _config = config;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FunctionIdRequirement requirement)
        {
            var jwksUri = _config["Jwt:JwksUri"];
            Uri uri = new Uri(jwksUri);
            var issuer = uri.Scheme + "://" + uri.Authority;
            var functionIdRole = context.User.FindFirst(r => r.Type == ClaimTypes.Role && r.Issuer == issuer);

            if(functionIdRole == null)
            {
                return Task.CompletedTask;
            }

            if( functionIdRole.Value == requirement.Id)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;

        }
    }
}
