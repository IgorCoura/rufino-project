using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateDependentEmployee
{
    public record CreateDependentEmployeeCommand(Guid EmployeeId, Guid CompanyId, string Name, IdCardModelCreateDependentEmployeeCommand IdCard, int Gender, int DependecyType) : IRequest<CreateDependentEmployeeResponse>
    {
    }
    public record IdCardModelCreateDependentEmployeeCommand(string Cpf, string MotherName, string FatherName, string BirthCity, string BirthState, string Nacionality, DateOnly DateOfBirth) { }
}
