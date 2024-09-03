using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateDependentEmployee
{
    public record CreateDependentEmployeeCommand(Guid EmployeeId, Guid CompanyId, string Name, IdCardModelCreateDependentEmployeeModel IdCard, int Gender, int DependecyType) : IRequest<CreateDependentEmployeeResponse>
    {
        public Dependent ToDependent() => Dependent.Create(Name, IdCard.ToIdCard(), Gender, DependecyType);
    }

    public record CreateDependentEmployeeModel(Guid EmployeeId, string Name, IdCardModelCreateDependentEmployeeModel IdCard, int Gender, int DependecyType)
    {
        public CreateDependentEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, Name, IdCard, Gender, DependecyType);
    }

    public record IdCardModelCreateDependentEmployeeModel(string Cpf, string MotherName, string FatherName, string BirthCity, 
        string BirthState, string Nacionality, DateOnly DateOfBirth) 
    {
        public IdCard ToIdCard() => IdCard.Create(Cpf, MotherName, FatherName, BirthCity, BirthState, Nacionality, DateOfBirth);
    }
}
