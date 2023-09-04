using Commom.Auth.Authorization;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Interfaces.Repositories;
using MaterialPurchase.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MaterialPurchase.API.Authorization
{
    public class PermissionAccessHandler : AuthorizationHandler<AuthorizationIdRequirement, ModelBase>
    {
        private readonly IConstructionRepository _constructionRepository;
        public PermissionAccessHandler(IConstructionRepository constructionRepository, ICompanyRepository companyRepository)
        {
            _constructionRepository = constructionRepository;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizationIdRequirement requirement, ModelBase resource)
        {
            var id = Guid.Parse(context.User.FindFirstValue(ClaimTypes.Sid));

            Construction? construction = await _constructionRepository.FirstAsync(x => x.Id == resource.ConstructionId, 
                include: i => i.Include(o => o.CompanyPermissions).ThenInclude(o => o.UsePermissions).ThenInclude(o => o.FunctionsIds));


            if(construction is null)
            {
                context.Fail();
                return;
            }
                

            var havePermission = construction.CompanyPermissions
                .Any(x => x.Id == resource.CompanyId && x.UsePermissions.Any(x => x.UserId == id && x.FunctionsIds.Any(x => x.Name == requirement.Id)));

            if (havePermission)
            {
                context.Succeed(requirement);
                return;
            }
            context.Fail();
            return;
        }
    }
}
