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
    [Route("api/v1/[controller]")]
    public class EmployeeController(ILogger<CompanyController> logger, IMediator mediator) : BaseController(logger)
    {
        [HttpPost("Create")]
        public async Task<ActionResult<CreateEmployeeResponse>> Create([FromBody] CreateEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
                var command = new IdentifiedCommand<CreateEmployeeCommand, CreateEmployeeResponse>(request, requestId);

                SendingCommandLog(request.Name, request, requestId);

                var result = await mediator.Send(command);

                CommandResultLog(result, request.Name, request, requestId);

                return OkResponse(result);

        }

        [HttpPut("Alter/Address")]
        public async Task<ActionResult<CreateEmployeeResponse>> AlterAddress([FromBody] AlterAddressEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterAddressEmployeeCommand, AlterAddressEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Alter/Contact")]
        public async Task<ActionResult<AlterContactEmployeeResponse>> AlterContact([FromBody] AlterContactEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterContactEmployeeCommand, AlterContactEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Alter/Dependent")]
        public async Task<ActionResult<AlterDependentEmployeeResponse>> AlterDependent([FromBody] AlterDependentEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterDependentEmployeeCommand, AlterDependentEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Alter/IdCard")]
        public async Task<ActionResult<AlterIdCardEmployeeResponse>> AlterIdCard([FromBody] AlterIdCardEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterIdCardEmployeeCommand, AlterIdCardEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Alter/MedicalAdmissionExam")]
        public async Task<ActionResult<AlterMedicalAdmissionExamEmployeeResponse>> AlterMedicalAdmissionExam([FromBody] AlterMedicalAdmissionExamEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterMedicalAdmissionExamEmployeeCommand, AlterMedicalAdmissionExamEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Alter/MilitarDocument")]
        public async Task<ActionResult<AlterMilitarDocumentEmployeeResponse>> AlterMilitarDocument([FromBody] AlterMilitarDocumentEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterMilitarDocumentEmployeeCommand, AlterMilitarDocumentEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Alter/Name")]
        public async Task<ActionResult<AlterNameEmployeeResponse>> AlterName([FromBody] AlterNameEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterNameEmployeeCommand, AlterNameEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Alter/PersonalInfo")]
        public async Task<ActionResult<AlterPersonalInfoEmployeeResponse>> AlterPersonalInfo([FromBody] AlterPersonalInfoEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterPersonalInfoEmployeeCommand, AlterPersonalInfoEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Alter/Role")]
        public async Task<ActionResult<AlterRoleEmployeeResponse>> AlterRole([FromBody] AlterRoleEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterRoleEmployeeCommand, AlterRoleEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Alter/VoteId")]
        public async Task<ActionResult<AlterVoteIdEmployeeResponse>> AlterVoteId([FromBody] AlterVoteIdEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterVoteIdEmployeeCommand, AlterVoteIdEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Alter/WorkPlace")]
        public async Task<ActionResult<AlterWorkPlaceEmployeeResponse>> AlterWorkPlace([FromBody] AlterWorkPlaceEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterWorkPlaceEmployeeCommand, AlterWorkPlaceEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Complete/Admission")]
        public async Task<ActionResult<CompleteAdmissionEmployeeResponse>> CompleteAdmission([FromBody] CompleteAdmissionEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CompleteAdmissionEmployeeCommand, CompleteAdmissionEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPost("Create/Dependent")]
        public async Task<ActionResult<CreateDependentEmployeeResponse>> CreateDependent([FromBody] CreateDependentEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateDependentEmployeeCommand, CreateDependentEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Finished/Contract")]
        public async Task<ActionResult<FinishedContractEmployeeResponse>> FinishedContract([FromBody] FinishedContractEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<FinishedContractEmployeeCommand, FinishedContractEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }
    }
}
