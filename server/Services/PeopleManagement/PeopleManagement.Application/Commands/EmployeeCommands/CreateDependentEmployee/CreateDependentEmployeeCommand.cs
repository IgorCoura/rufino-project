using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateDependentEmployee
{
    public record CreateDependentEmployeeCommand(Guid EmployeeId, Guid CompanyId, string Name, IdCardModelCreateDependentEmployeeModel IdCard, int Gender, int DependencyType) : IRequest<CreateDependentEmployeeResponse>
    {
        public Dependent ToDependent() => Dependent.Create(Name, IdCard.ToIdCard(), Gender, DependencyType);
    }

    public record CreateDependentEmployeeModel(Guid EmployeeId, string Name, IdCardModelCreateDependentEmployeeModel IdCard, int Gender, int DependencyType)
    {
        public CreateDependentEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, Name, IdCard, Gender, DependencyType);
    }

    public record IdCardModelCreateDependentEmployeeModel(string Cpf, string MotherName, string FatherName, string BirthCity, 
        string BirthState, string Nacionality, DateOnly DateOfBirth) 
    {
        public IdCard ToIdCard() => IdCard.Create(Cpf, MotherName, FatherName, BirthCity, BirthState, Nacionality, DateOfBirth);
    }
}
