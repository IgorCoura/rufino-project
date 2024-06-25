using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CompleteAdmissionEmployee
{
    public record CompleteAdmissionEmployeeCommand(Guid EmployeeId, Guid CompanyId, string Registration, DateOnly dateInit, int ContractType) : IRequest<CompleteAdmissionEmployeeResponse>
    {
    }
}
