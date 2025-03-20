using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.Queries.Employee;
using PeopleManagement.Application.Commands.Queries.Role;
using static PeopleManagement.Application.Commands.Queries.Role.RoleDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class RoleController(ILogger<CompanyController> logger, IMediator mediator, IRoleQueries roleQueries) : BaseController(logger)
    {

        [HttpGet("{id}")]
        [ProtectedResource("role", "view")]
        public async Task<ActionResult<RoleDto>> GetEmployee([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await roleQueries.GetRole(id, company);
            return OkResponse(result);
        }
    }
}
