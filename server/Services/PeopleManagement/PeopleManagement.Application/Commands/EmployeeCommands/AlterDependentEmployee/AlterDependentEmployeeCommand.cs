using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using IdCard = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.IdCard;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee
{
    public record AlterDependentEmployeeCommand(Guid EmployeeId, Guid CompanyId, string OldName, DependentModelAlterDependentEmployeeModel CurrentDepentent) : IRequest<AlterDependentEmployeeResponse>
    {
    }
    public record AlterDependentEmployeeModel(Guid EmployeeId, string OldName, DependentModelAlterDependentEmployeeModel CurrentDepentent)
    {
        public AlterDependentEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, OldName, CurrentDepentent);
    }
    public record DependentModelAlterDependentEmployeeModel(string Name, IdCardModelAlterDependentEmployeeModel IdCard, int Gender, int DependecyType) 
    { 
        public Dependent ToDependent() => Dependent.Create(Name, IdCard.ToIdCard(), Gender, DependecyType);
    }
    public record IdCardModelAlterDependentEmployeeModel(string Cpf, string MotherName, string FatherName, string BirthCity, string BirthState, string Nacionality, DateOnly DateOfBirth) 
    { 
        public IdCard ToIdCard() => IdCard.Create(Cpf, MotherName, FatherName, BirthCity, BirthState, Nacionality, DateOfBirth);
    }
}
