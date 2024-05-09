using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterPersonalInfoEmployee
{
    public record AlterPersonalInfoEmployeeCommand(Guid EmployeeId, Guid CompanyId, DeficiencyModel Deficiency,  int MaritalStatus, int Gender, int Ethinicity, int EducationLevel) : IRequest<AlterPersonalInfoEmployeeResponse>
    {
    }
    public record DeficiencyModel(int[] Disability, string Observation);
}
