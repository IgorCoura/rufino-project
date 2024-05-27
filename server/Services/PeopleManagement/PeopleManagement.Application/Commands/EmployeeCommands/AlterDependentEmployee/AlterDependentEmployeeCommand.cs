using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using IdCard = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.IdCard;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee
{
    public record AlterDependentEmployeeCommand(Guid EmployeeId, Guid CompanyId, string OldName, DependentModelAlterDependentEmployeeCommand CurrentDepentent) : IRequest<AlterDependentEmployeeResponse>
    {
    }
    public record DependentModelAlterDependentEmployeeCommand(string Name, IdCardModelAlterDependentEmployeeCommand IdCard, int Gender, int DependecyType) 
    { 
        public Dependent ToDependent() => Dependent.Create(Name, IdCard.ToIdCard(), Gender, DependecyType);
    }
    public record IdCardModelAlterDependentEmployeeCommand(string Cpf, string MotherName, string FatherName, string BirthCity, string BirthState, string Nacionality, DateOnly DateOfBirth) 
    { 
        public IdCard ToIdCard() => IdCard.Create(Cpf, MotherName, FatherName, BirthCity, BirthState, Nacionality, DateOfBirth);
    }
}
