using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Queries.Workplace;
using static PeopleManagement.Application.Queries.Workplace.WorkplaceDtos;

namespace PeopleManagement.API.Controllers
{


    [Route("api/v1/{company}/[controller]")]
    public class WorkplaceController(ILogger<WorkplaceController> logger, IMediator mediator, IWorkplaceQueries workplaceQueries) : BaseController(logger)
    {

        [HttpGet("{workplaceId}")]
        [ProtectedResource("workplace", "view")]
        public async Task<ActionResult<WorkplaceDto>> GetById([FromRoute] Guid workplaceId, [FromRoute] Guid company)
        {
            var result = await workplaceQueries.GetByIdAsync(workplaceId, company);
            return OkResponse(result);
        }

        [HttpGet]
        [ProtectedResource("workplace", "view")]
        public async Task<ActionResult<WorkplaceDto>> GetAll([FromRoute] Guid company)
        {
            var result = await workplaceQueries.GetAllAsync(company);
            return OkResponse(result);
        }
    }
}
