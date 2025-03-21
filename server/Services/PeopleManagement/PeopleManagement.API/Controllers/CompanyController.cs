using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.CompanyCommands.CreateCompany;
using PeopleManagement.Application.Commands.DTO;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Queries.Company;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class CompanyController(IMediator mediator, ICompanyQueries companyQueries, ILogger<CompanyController> logger) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost]
        [ProtectedResource("company", "create")]
        public async Task<ActionResult<BaseDTO>> CreateCompany([FromBody] CreateCompanyCommand request, [FromHeader(Name = "x-requestid")] string requestId)
        {
           
            if (Guid.TryParse(requestId, out Guid guid) && guid != Guid.Empty)
            {
                var command = new IdentifiedCommand<CreateCompanyCommand, BaseDTO>(request, guid);

                SendingCommandLog(request.Cnpj, request, guid);

                var result = await _mediator.Send(command);

                CommandResultLog(result, request.Cnpj, request, guid);

                return OkResponse(result);
            }
            else
            {
                return BadRequestResponse("Invalid request Id");
            }

            
        }

        [HttpGet]
        [ProtectedResource("company", "view")]
        public async Task<ActionResult<CompanySimplefiedDTO>> GetCompany([FromQuery] Guid id)
        {
            var company = await companyQueries.GetCompanySimplefiedAsync(id);
            return OkResponse(company);
        }

        [HttpGet("list")]
        [ProtectedResource("company", "view")]
        public async Task<ActionResult<IEnumerable<CompanySimplefiedDTO>>> GetCompanies([FromQuery] Guid[] id)
        {
            var companies = await companyQueries.GetCompaniesSimplefiedsAsync(id);
            return OkResponse(companies);
        }
    }
}
