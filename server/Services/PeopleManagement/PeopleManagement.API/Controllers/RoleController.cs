using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Queries.Role;
using static PeopleManagement.Application.Queries.Role.RoleDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class RoleController(ILogger<RoleController> logger, IMediator mediator, IRoleQueries roleQueries) : BaseController(logger)
    {

        [HttpGet("{id}")]
        [ProtectedResource("role", "view")]
        public async Task<ActionResult<RoleDto>> GetRole([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await roleQueries.GetRole(id, company);
            return OkResponse(result);
        }
        
        [HttpGet("all/simple/{positionId}")]
        [ProtectedResource("role", "view")]
        public async Task<ActionResult<RoleDto>> GetAllSimpleRoles([FromRoute] Guid company, [FromRoute] Guid positionId)
        {
            var result = await roleQueries.GetAllSimpleRoles(positionId, company);
            return OkResponse(result);
        }
    }
}
