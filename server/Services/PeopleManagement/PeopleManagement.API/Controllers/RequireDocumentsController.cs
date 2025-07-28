using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments;
using PeopleManagement.Application.Commands.RequireDocumentsCommands.EditRequireSecurityDocuments;
using PeopleManagement.Application.Queries.RequireDocuments;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using static PeopleManagement.Application.Queries.RequireDocuments.RequireDocumentsDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class RequireDocumentsController(ILogger<RequireDocumentsController> logger, IMediator mediator, IRequireDocumentsQueries requireDocumentsQueries) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;
        private readonly IRequireDocumentsQueries _requireDocumentsQueries = requireDocumentsQueries;

        [HttpPost]
        [ProtectedResource("RequireDocuments", "create")]
        public async Task<ActionResult<CreateRequireDocumentsResponse>> Create([FromRoute] Guid company, [FromBody] CreateRequireDocumentsModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateRequireDocumentsCommand, CreateRequireDocumentsResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.AssociationId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.AssociationId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut]
        [ProtectedResource("RequireDocuments", "edit")]
        public async Task<ActionResult<EditRequireDocumentsResponse>> Edit([FromRoute] Guid company, [FromBody] EditRequireDocumentsModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<EditRequireDocumentsCommand, EditRequireDocumentsResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.AssociationId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.AssociationId, request, requestId);

            return OkResponse(result);
        }

        [HttpGet]
        [ProtectedResource("RequireDocuments", "view")]
        public async Task<ActionResult<IEnumerable<RequireDocumentSimpleDto>>> GetAll([FromRoute] Guid company)
        {
            var result = await _requireDocumentsQueries.GetAllSimple(company);
            return OkResponse(result);
        }

        [HttpGet("{id}")]
        [ProtectedResource("RequireDocuments", "view")]
        public async Task<ActionResult<RequireDocumentDto>> GetAll([FromRoute] Guid id, [FromRoute] Guid company)
        {
            var result = await _requireDocumentsQueries.GetById(id, company);
            return OkResponse(result);
        }


        [HttpGet("association/{associationTypeId}")]
        [ProtectedResource("RequireDocuments", "view")]
        public async Task<ActionResult<IEnumerable<AssociationDto>>> GetAllAssociationsByType([FromRoute] Guid company, [FromRoute] int associationTypeId)
        {
            var result = await _requireDocumentsQueries.GetAllAssociationsByType(company, associationTypeId);
            return OkResponse(result);
        }

        [HttpGet("associationType")]
        [ProtectedResource("RequireDocuments", "view")]
        public ActionResult<IEnumerable<AssociationType>> GetAllAssociationsType([FromRoute] Guid company)
        {
            var result = AssociationType.GetAll<AssociationType>();
            return OkResponse(result);
        }

        [HttpGet("withdocuments/{employeeId}")]
        [ProtectedResource("RequireDocuments", "view")]
        public async Task<ActionResult<IEnumerable<RequiredWithDocumentListDto>>> GetAllWithDocumentList([FromRoute] Guid company, [FromRoute] Guid employeeId)
        {
            var result = await _requireDocumentsQueries.GetAllWithDocumentList(company, employeeId);
            return OkResponse(result);
        }


        [HttpGet("events")]
        [ProtectedResource("RequireDocuments", "view")]
        public ActionResult<IEnumerable<EmployeeEvent>> GetEvents([FromRoute] Guid company)
        {
            var result = RecurringEvents.GetAll();
            return OkResponse(result);
        }

    }
}
