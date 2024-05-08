namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee
{
    public record AlterDependentEmployeeCommand(Guid Id, string OldName, DependentModel CurrentDepentent) : IRequest<AlterDependentEmployeeResponse>
    {
    }
    public record DependentModel(string Name, IdCardModel IdCard, int Gender, int DependecyType) { }
    public record IdCardModel(string Cpf, string MotherName, string FatherName, string BirthCity, string BirthState, string Nacionality, DateOnly DateOfBirth) { }
}
