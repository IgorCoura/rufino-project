using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using IdCard = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.IdCard;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee
{
    public record AlterDependentEmployeeCommand(Guid EmployeeId, Guid CompanyId, string OldName, DependentModelAlterDependentEmployeeModel CurrentDependent) : IRequest<AlterDependentEmployeeResponse>
    {
    }
    public record AlterDependentEmployeeModel(Guid EmployeeId, string OldName, DependentModelAlterDependentEmployeeModel CurrentDependent)
    {
        public AlterDependentEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, OldName, CurrentDependent);
    }
    public record DependentModelAlterDependentEmployeeModel(string Name, IdCardModelAlterDependentEmployeeModel IdCard, int Gender, int DependencyType) 
    { 
        public Dependent ToDependent() => Dependent.Create(Name, IdCard.ToIdCard(), Gender, DependencyType);
    }
    public record IdCardModelAlterDependentEmployeeModel(string Cpf, string MotherName, string FatherName, string BirthCity, string BirthState, string Nacionality, DateOnly DateOfBirth) 
    { 
        public IdCard ToIdCard() => IdCard.Create(Cpf, MotherName, FatherName, BirthCity, BirthState, Nacionality, DateOfBirth);
    }
}
