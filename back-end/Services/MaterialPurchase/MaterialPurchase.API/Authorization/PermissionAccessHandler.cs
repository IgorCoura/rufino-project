using Commom.API.AuthorizationIds;
using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Interfaces.Repositories;
using MaterialPurchase.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;

namespace MaterialPurchase.API.Authorization
{
    public class PermissionAccessHandler : AuthorizationHandler<AuthorizationIdRequirement, ModelBase>
    {
        private readonly IConstructionRepository _constructionRepository;
        public PermissionAccessHandler(IConstructionRepository constructionRepository)
        {
            _constructionRepository = constructionRepository;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizationIdRequirement requirement, ModelBase resource)
        {
            var id = Guid.Parse(context.User.FindFirstValue(ClaimTypes.Sid));
            Construction construction = await _constructionRepository.FirstAsync(x => x.Id == resource.ConstructionId) 
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(resource.ConstructionId), resource.ConstructionId.ToString());

            var havePermission = construction.UsePermissions.Any(x => x.UserId == id && x.FunctionsIds.Any(x => x.Name == requirement.Id));

            if(havePermission)
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
