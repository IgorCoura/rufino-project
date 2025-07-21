using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.RoleCommands.CreateRole;
using PeopleManagement.Application.Commands.RoleCommands.EditRole;
using PeopleManagement.Application.Queries.Role;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.SeedWord;
using static PeopleManagement.Application.Queries.Role.RoleDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class RoleController(ILogger<RoleController> logger, IMediator mediator, IRoleQueries roleQueries) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;
        private readonly IRoleQueries _roleQueries = roleQueries;

        [HttpPost]
        [ProtectedResource("role", "create")]
        public async Task<ActionResult<CreateRoleResponse>> Create([FromRoute] Guid company, [FromBody] CreateRoleModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateRoleCommand, CreateRoleResponse>(request.ToCommand(company), requestId);
            SendingCommandLog(request.Name, request, requestId);
            var result = await _mediator.Send(command);
            CommandResultLog(result, request.Name, request, requestId);
            return OkResponse(result);
        }

        [HttpPut]
        [ProtectedResource("role", "edit")]
        public async Task<ActionResult<EditRoleResponse>> Edit([FromRoute] Guid company, [FromBody] EditRoleModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<EditRoleCommand, EditRoleResponse>(request.ToCommand(company), requestId);
            SendingCommandLog(request.Id, request, requestId);
            var result = await _mediator.Send(command);
            CommandResultLog(result, request.Id, request, requestId);
            return OkResponse(result);
        }

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

        [HttpGet("paymentUnit")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<PaymentUnit>> GetPaymentUnit([FromRoute] Guid company)
        {
            var result = Enumeration.GetAll<PaymentUnit>();
            return OkResponse(result);
        }

        [HttpGet("currencyType")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<CurrencyType>> GetCurrencyTypee([FromRoute] Guid company)
        {
            var result = Enumeration.GetAll<CurrencyType>();
            return OkResponse(result);
        }
    }
}
