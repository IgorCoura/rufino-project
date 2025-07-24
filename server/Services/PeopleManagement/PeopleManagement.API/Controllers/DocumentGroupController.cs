using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.DepartmentCommands.CreateDepartment;
using PeopleManagement.Application.Commands.DepartmentCommands.EditDepartment;
using PeopleManagement.Application.Commands.DocumentGroupCommands.CreateDocumentGroup;
using PeopleManagement.Application.Commands.DocumentGroupCommands.EditDocumentGroup;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Queries.Department;
using PeopleManagement.Application.Queries.DocumentGroup;
using static PeopleManagement.Application.Queries.Department.DepartmentDtos;
using static PeopleManagement.Application.Queries.DocumentGroup.DocumentGroupDtos;
using static PeopleManagement.Application.Queries.RequireDocuments.RequireDocumentsDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class DocumentGroupController(ILogger<DepartmentController> logger, IMediator mediator, IDocumentGroupQueries queries) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;
        private readonly IDocumentGroupQueries _queries = queries;

        [HttpPost]
        [ProtectedResource("DocumentGroup", "create")]
        public async Task<ActionResult<CreateDocumentGroupResponse>> Create([FromRoute] Guid company, [FromBody] CreateDocumentGroupModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateDocumentGroupCommand, CreateDocumentGroupResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.Name, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.Name, request, requestId);

            return OkResponse(result);
        }

        [HttpPut]
        [ProtectedResource("DocumentGroup", "edit")]
        public async Task<ActionResult<EditDocumentGroupResponse>> Edit([FromRoute] Guid company, [FromBody] EditDocumentGroupModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<EditDocumentGroupCommand, EditDocumentGroupResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.Id, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.Id, request, requestId);

            return OkResponse(result);
        }

        [HttpGet]
        [ProtectedResource("DocumentGroup", "view")]
        public async Task<ActionResult<IEnumerable<DocumentGroupDto>>> GetAll([FromRoute] Guid company)
        {
            var result = await _queries.GetAll(company);
            return OkResponse(result);
        }

        [HttpGet("withdocuments/{employeeId}")]
        [ProtectedResource("DocumentGroup", "view")]
        public async Task<ActionResult<IEnumerable<DocumentGroupWithDocumentsDto>>> GetAllWithDocuments([FromRoute] Guid company, [FromRoute] Guid employeeId)
        {
            var result = await _queries.GetAllWithDocuments(company, employeeId);
            return OkResponse(result);
        }

    }
}
