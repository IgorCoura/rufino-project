using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using System.Text.Json.Nodes;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using System.Text.Json;

namespace PeopleManagement.Services.Services.RecoverInfoToDocument
{
    public class RecoverEmployeeInfoToDocumentTemplateService(IEmployeeRepository employeeRepository) : IRecoverEmployeeInfoToDocumentTemplateService
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        public async Task<JsonObject> RecoverInfo(Guid employeeId, Guid companyId, CancellationToken cancellation = default)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == employeeId && x.CompanyId == companyId, cancellation: cancellation)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), employeeId.ToString()));

            var employeeJson = new JsonObject
            {
                ["Id"] = employee.Id.ToString(),
                ["Name"] = employee.Name?.ToString(),
                ["Address"] = JsonSerializer.Serialize(employee.Address),
                ["Contact"] = JsonSerializer.Serialize(employee.Contact),
                ["Status"] = employee.Status.ToString(),
                ["MedicalAdmissionExam"] = JsonSerializer.Serialize(employee.MedicalAdmissionExam),
                ["PersonalInfo"] = JsonSerializer.Serialize(employee.PersonalInfo),
                ["IdCard"] = JsonSerializer.Serialize(employee.IdCard),
                ["VoteId"] = JsonSerializer.Serialize(employee.VoteId),
                ["MilitaryDocument"] = JsonSerializer.Serialize(employee.MilitaryDocument)
            };

            return employeeJson;
        }
    }
}
