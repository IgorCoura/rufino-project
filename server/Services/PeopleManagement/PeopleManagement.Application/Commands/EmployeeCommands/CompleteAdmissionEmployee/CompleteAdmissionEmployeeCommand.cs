using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CompleteAdmissionEmployee
{
    public record CompleteAdmissionEmployeeCommand(Guid EmployeeId, Guid CompanyId, string Registration, DateOnly dateInit, int ContractType) : IRequest<CompleteAdmissionEmployeeResponse>
    {
    }

    public record CompleteAdmissionEmployeeModel(Guid EmployeeId, string Registration, DateOnly dateInit, int ContractType)
    {
        public CompleteAdmissionEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, Registration, dateInit, ContractType);
    }
}
