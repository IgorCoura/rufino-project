using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterAddressEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterContactEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterIdCardEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterMedicalAdmissionExamEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterMilitarDocumentEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterNameEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterPersonalInfoEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterRoleEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterVoteIdEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterWorkPlaceEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.CompleteAdmissionEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.CreateDependentEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.FinishedContractEmployee;
using PeopleManagement.Application.Commands.Identified;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class EmployeeController(ILogger<CompanyController> logger, IMediator mediator) : BaseController(logger)
    {
        [HttpPost]
        [ProtectedResource("employee", "create")]
        public async Task<ActionResult<CreateEmployeeResponse>> Create([FromRoute] Guid company, [FromBody] CreateEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
                var command = new IdentifiedCommand<CreateEmployeeCommand, CreateEmployeeResponse>(request.ToCommand(company), requestId);

                SendingCommandLog(request.Name, request, requestId);

                var result = await mediator.Send(command);

                CommandResultLog(result, request.Name, request, requestId);

                return OkResponse(result);

        }

        [HttpPut("Address")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<CreateEmployeeResponse>> AlterAddress([FromRoute] Guid company, [FromBody] AlterAddressEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterAddressEmployeeCommand, AlterAddressEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Contact")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<AlterContactEmployeeResponse>> AlterContact([FromRoute] Guid company, [FromBody] AlterContactEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterContactEmployeeCommand, AlterContactEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Dependent")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<AlterDependentEmployeeResponse>> AlterDependent([FromRoute] Guid company, [FromBody] AlterDependentEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterDependentEmployeeCommand, AlterDependentEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("IdCard")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<AlterIdCardEmployeeResponse>> AlterIdCard([FromRoute] Guid company, [FromBody] AlterIdCardEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterIdCardEmployeeCommand, AlterIdCardEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("MedicalAdmissionExam")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<AlterMedicalAdmissionExamEmployeeResponse>> AlterMedicalAdmissionExam([FromRoute] Guid company, 
            [FromBody] AlterMedicalAdmissionExamEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterMedicalAdmissionExamEmployeeCommand, AlterMedicalAdmissionExamEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("MilitarDocument")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<AlterMilitarDocumentEmployeeResponse>> AlterMilitarDocument([FromRoute] Guid company, 
            [FromBody] AlterMilitarDocumentEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterMilitarDocumentEmployeeCommand, AlterMilitarDocumentEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Name")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<AlterNameEmployeeResponse>> AlterName([FromRoute] Guid company, [FromBody] AlterNameEmployeeModel request, 
            [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterNameEmployeeCommand, AlterNameEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("PersonalInfo")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<AlterPersonalInfoEmployeeResponse>> AlterPersonalInfo([FromRoute] Guid company, 
            [FromBody] AlterPersonalInfoEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterPersonalInfoEmployeeCommand, AlterPersonalInfoEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Role")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<AlterRoleEmployeeResponse>> AlterRole([FromRoute] Guid company, [FromBody] AlterRoleEmployeeModel request, 
            [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterRoleEmployeeCommand, AlterRoleEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("VoteId")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<AlterVoteIdEmployeeResponse>> AlterVoteId([FromRoute] Guid company, [FromBody] AlterVoteIdEmployeeModel request, 
            [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterVoteIdEmployeeCommand, AlterVoteIdEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("WorkPlace")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<AlterWorkPlaceEmployeeResponse>> AlterWorkPlace([FromRoute] Guid company, 
            [FromBody] AlterWorkPlaceEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterWorkPlaceEmployeeCommand, AlterWorkPlaceEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Admission/Complete")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<CompleteAdmissionEmployeeResponse>> CompleteAdmission([FromRoute] Guid company, 
            [FromBody] CompleteAdmissionEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CompleteAdmissionEmployeeCommand, CompleteAdmissionEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPost("Dependent")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<CreateDependentEmployeeResponse>> CreateDependent([FromRoute] Guid company, 
            [FromBody] CreateDependentEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateDependentEmployeeCommand, CreateDependentEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Contract/Finished")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<FinishedContractEmployeeResponse>> FinishedContract([FromRoute] Guid company, 
            [FromBody] FinishedContractEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<FinishedContractEmployeeCommand, FinishedContractEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }
    }
}
