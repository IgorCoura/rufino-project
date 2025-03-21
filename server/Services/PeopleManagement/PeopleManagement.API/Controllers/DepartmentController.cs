using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Queries.Department;
using static PeopleManagement.Application.Queries.Department.DepartmentDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class DepartmentController(ILogger<DepartmentController> logger, IMediator mediator, IDepartmentQueries departmentQueries) : BaseController(logger)
    {

        [HttpGet("all")]
        [ProtectedResource("department", "view")]
        public async Task<ActionResult<DepartmentDto>> GetAll([FromRoute] Guid company)
        {
            var result = await departmentQueries.GetAll(company);
            return OkResponse(result);
        }

        [HttpGet("all/simple")]
        [ProtectedResource("department", "view")]
        public async Task<ActionResult<DepartmentSimpleDto>> GetAllSimple([FromRoute] Guid company)
        {
            var result = await departmentQueries.GetAll(company);
            return OkResponse(result);
        }
    }
}
