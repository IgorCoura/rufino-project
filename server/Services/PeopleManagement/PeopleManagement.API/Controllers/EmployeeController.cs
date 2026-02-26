using Hangfire;
using Microsoft.Extensions.Logging;
using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocument;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterAddressEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterContactEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterIdCardEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterImageEmployee;
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
using PeopleManagement.Application.Commands.EmployeeCommands.DocumentSigningOptions;
using PeopleManagement.Application.Commands.EmployeeCommands.FinishedContractEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.IsRequiredMilitaryDocumentEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.MarkEmployeeAsInactive;
using PeopleManagement.Application.Commands.EmployeeCommands.RemoveDependentEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Queries.Employee;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Domain.SeedWord;
using System.Diagnostics;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class EmployeeController(ILogger<EmployeeController> logger, IMediator mediator, IEmployeeQueries employeeQueries) : BaseController(logger)
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

        [HttpPut("MilitaryDocument")]
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

        [HttpPut("Dependent/Create")]
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

        [HttpPut("Dependent/Edit")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<AlterDependentEmployeeResponse>> AlterDependent([FromRoute] Guid company, [FromBody] AlterDependentEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AlterDependentEmployeeCommand, AlterDependentEmployeeResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("Dependent/Remove")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<RemoveDependentEmployeeResponse>> RemoveDependent([FromRoute] Guid company, [FromBody] RemoveDependentEmployeeModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<RemoveDependentEmployeeCommand, RemoveDependentEmployeeResponse>(request.ToCommand(company), requestId);

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

        [HttpPut("DocumentSigningOptions")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<DocumentSigningOptionsResponse>> DocumentSigningOptions([FromRoute] Guid company,
            [FromBody] DocumentSigningOptionsModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<DocumentSigningOptionsCommand, DocumentSigningOptionsResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }


        [HttpPut("mark-as-inactive")]
        [ProtectedResource("employee", "edit")]
        public async Task<ActionResult<MarkEmployeeAsInactiveResponse>> MarkAsInactive([FromRoute] Guid company, [FromBody] MarkEmployeeAsInactiveModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<MarkEmployeeAsInactiveCommand, MarkEmployeeAsInactiveResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("image/{employeeId}")]
        [ProtectedResource("Document", "send")]
        [RequestSizeLimit(12_000_000)]
        public async Task<ActionResult<InsertDocumentResponse>> Insert(IFormFile formFile, [FromRoute] Guid company, [FromRoute] Guid employeeid, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var extension = Path.GetExtension(formFile.FileName);
            var stream = formFile.OpenReadStream();
            var request = new AlterImageEmployeeCommand(employeeid, company, extension, stream);

            var command = new IdentifiedCommand<AlterImageEmployeeCommand, AlterImageEmployeeResponse>(request, requestId);

            SendingCommandLog(request.EmployeeId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.EmployeeId, request, requestId);

            return OkResponse(result);
        }


        [HttpGet("list")]
        [ProtectedResource("employee", "view")]
        public async Task<ActionResult<IEnumerable<EmployeeWithRoleAndDocumentStatusDto>>> GetEmployeesWithRoleAndDocumentStatus([FromRoute] Guid company, [FromQuery] EmployeeParams employeeParams)
        {
            var result = await employeeQueries.GetEmployeeListWithRolesAndDocumentStatus(employeeParams, company);
            return OkResponse(result);
        }

        [HttpGet("{id}")]
        [ProtectedResource("employee", "view")]
        public async Task<ActionResult<EmployeeDto>> GetEmployee([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await employeeQueries.GetEmployee(id, company);
            return OkResponse(result);
        }

        [HttpGet("contact/{id}")]
        [ProtectedResource("employee", "view")]
        public async Task<ActionResult<EmployeeAddressDto>> GetEmployeeContact([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await employeeQueries.GetEmployeeContact(id, company);
            return OkResponse(result);
        }

        [HttpGet("address/{id}")]
        [ProtectedResource("employee", "view")]
        public async Task<ActionResult<EmployeeAddressDto>> GetEmployeeAddress([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await employeeQueries.GetEmployeeAddress(id, company);
            return OkResponse(result);
        }

        [HttpGet("personalinfo/{id}")]
        [ProtectedResource("employee", "view")]
        public async Task<ActionResult<EmployeePersonalInfoDto>> GetEmployeePersonalInfo([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await employeeQueries.GetEmployeePersonalInfo(id, company);
            return OkResponse(result);
        }
        
        [HttpGet("idcard/{id}")]
        [ProtectedResource("employee", "view")]
        public async Task<ActionResult<EmployeeIdCardDto>> GetEmployeeIdCard([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await employeeQueries.GetEmployeeIdCard(id, company);
            return OkResponse(result);
        }

        [HttpGet("voteid/{id}")]
        [ProtectedResource("employee", "view")]
        public async Task<ActionResult<EmployeeVoteIdDto>> GetEmployeeVoteId([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await employeeQueries.GetEmployeeVoteId(id, company);
            return OkResponse(result);
        }

        [HttpGet("militarydocument/{id}")]
        [ProtectedResource("employee", "view")]
        public async Task<ActionResult<EmployeeMilitaryDocumentDto>> GetEmployeeMilitaryDocument([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var command = new IsRequiredMilitaryDocumentEmployeeCommand(id, company);
            var documentIsRequired = await mediator.Send(command);
            var result = await employeeQueries.GetEmployeeMilitaryDocument(id, company, documentIsRequired.IsRequired);
            return OkResponse(result);
        }


        [HttpGet("dependents/{id}")]
        [ProtectedResource("employee", "view")]
        public async Task<ActionResult<EmployeeDependentsDto>> GetEmployeeDependents([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await employeeQueries.GetEmployeeDependents(id, company);
            return OkResponse(result);
        }



        [HttpGet("status")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<Status>> GetStatus([FromRoute] Guid company)
        {
            var result = Status.GetAll<Status>();
            return OkResponse(result);
        }

        [HttpGet("events")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<EmployeeEvent>> GetEvents([FromRoute] Guid company)
        {
            var result = EmployeeEvent.GetAll();
            return OkResponse(result);
        }

        [HttpGet("maritalstatus")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<MaritalStatus>> GetMaritalStatus([FromRoute] Guid company)
        {
            var result = MaritalStatus.GetAll<MaritalStatus>();
            return OkResponse(result);
        }

        [HttpGet("disability")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<Disability>> GetDisability([FromRoute] Guid company)
        {
            var result = Disability.GetAll<Disability>();
            return OkResponse(result);
        }

        [HttpGet("gender")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<Gender>> GetGender([FromRoute] Guid company)
        {
            var result = Disability.GetAll<Gender>();
            return OkResponse(result);
        }

        [HttpGet("DocumentSigningOptions")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<Gender>> GetDocumentSigningOptions([FromRoute] Guid company)
        {
            var result = Enumeration.GetAll<DocumentSigningOptions>();
            return OkResponse(result);
        }

        [HttpGet("dependencytype")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<DependencyType>> GetDependencyType([FromRoute] Guid company)
        {
            var result = Disability.GetAll<DependencyType>();
            return OkResponse(result);
        }

        [HttpGet("personalinfo/selectionoptions")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<Disability>> GetPersonalInfoSelectionOptions([FromRoute] Guid company)
        {
            var result = new
            {
                MaritalStatus = Disability.GetAll<MaritalStatus>(),
                Gender = Disability.GetAll<Gender>(),
                Ethinicity = Disability.GetAll<Ethinicity>(),
                EducationLevel = Disability.GetAll<EducationLevel>(),
                Disability = Disability.GetAll<Disability>()
            };
  
            return OkResponse(result);
        }

        [HttpGet("MedicalAdmissionExam/{id}")]
        [ProtectedResource("employee", "view")]
        public async Task<ActionResult<EmployeeAddressDto>> GetEmployeeMedicalAdmissionExam([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await employeeQueries.GetEmployeeMedicalAdmissionExam(id, company);
            return OkResponse(result);
        }
        
        [HttpGet("Contracts/{id}")]
        [ProtectedResource("employee", "view")]
        public async Task<ActionResult<EmployeeContractsDto>> GetEmployeeContracts([FromRoute] Guid company, [FromRoute] Guid id)
        {
            var result = await employeeQueries.GetEmployeeContracts(id, company);
            return OkResponse(result);
        }

        [HttpGet("Contracts/types")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<EmploymentContractType>> GetContractsType([FromRoute] Guid company)
        {
            var result = EmploymentContractType.GetAll<EmploymentContractType>();
            return OkResponse(result);
        }

        [HttpGet("DocumentStatus")]
        [ProtectedResource("employee", "view")]
        public ActionResult<IEnumerable<EmployeeDocumentStatus>> GetDocumentStatus([FromRoute] Guid company)
        {
            var result = EmployeeDocumentStatus.GetAll<EmployeeDocumentStatus>();
            return OkResponse(result);
        }

        [HttpGet("image/{employeeId}")]
        [ProtectedResource("Document", "view")]
        public async Task<IActionResult> DownloadFile([FromRoute] Guid employeeId,
            [FromRoute] Guid company)
        {
            var img = await employeeQueries.DownloadImage(employeeId, company);
            return File(img.stream, "application/octet-stream", $"img-{employeeId}.{img.Extension}");
        }
    }
}
