using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.DepartmentCommands.CreateDepartment;
using PeopleManagement.Application.Commands.DepartmentCommands.EditDepartment;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Queries.Department;
using static PeopleManagement.Application.Queries.Department.DepartmentDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class DepartmentController(ILogger<DepartmentController> logger, IMediator mediator, IDepartmentQueries departmentQueries) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;
        private readonly IDepartmentQueries _departmentQueries = departmentQueries;

        [HttpPost]
        [ProtectedResource("Department", "create")]
        public async Task<ActionResult<CreateDepartmentResponse>> Create([FromRoute] Guid company, [FromBody] CreateDepartmentModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateDepartmentCommand, CreateDepartmentResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.Name, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.Name, request, requestId);

            return OkResponse(result);
        }

        [HttpPut]
        [ProtectedResource("Department", "edit")]
        public async Task<ActionResult<EditDepartmentResponse>> Edit([FromRoute] Guid company, [FromBody] EditDepartmentModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<EditDepartmentCommand, EditDepartmentResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.Id, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.Id, request, requestId);

            return OkResponse(result);
        }

        [HttpGet("all")]
        [ProtectedResource("Department", "view")]
        public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAll([FromRoute] Guid company)
        {
            var result = await _departmentQueries.GetAll(company);
            return OkResponse(result);
        }

        [HttpGet("all/simple")]
        [ProtectedResource("Department", "view")]
        public async Task<ActionResult<IEnumerable<DepartmentSimpleDto>>> GetAllSimple([FromRoute] Guid company)
        {
            var result = await _departmentQueries.GetAll(company);
            return OkResponse(result);
        }

        [HttpGet("{id}")]
        [ProtectedResource("Department", "view")]
        public async Task<ActionResult<DepartmentSimpleDto>> GetAllSimple([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await _departmentQueries.GetById(id, company);
            return OkResponse(result);
        }
    }
}
