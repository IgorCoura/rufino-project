using PeopleManagement.API.Authorization;
using static PeopleManagement.Application.Queries.Department.DepartmentDtos;
using static PeopleManagement.Application.Queries.Position.PositionDtos;
using PeopleManagement.Application.Queries.Position;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class PositionController(ILogger<DepartmentController> logger, IMediator mediator, IPositionQueries positionQueries) : BaseController(logger)
    {

        [HttpGet("all/simple/{departmentId}")]
        [ProtectedResource("position", "view")]
        public async Task<ActionResult<PositionSimpleDto>> GetAllSimple([FromRoute] Guid departmentId, [FromRoute] Guid company)
        {
            var result = await positionQueries.GetAllSimple(departmentId, company);
            return OkResponse(result);
        }
    }
}
