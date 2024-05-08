using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateDependentEmployee
{
    public record CreateDependentEmployeeCommand(Guid Id, string Name, IdCardModel IdCard, int Gender, int DependecyType) : IRequest<CreateDependentEmployeeResponse>
    {
    }
    public record IdCardModel(string Cpf, string MotherName, string FatherName, string BirthCity, string BirthState, string Nacionality, DateOnly DateOfBirth) { }
}
