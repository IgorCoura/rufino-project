using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterIdCardEmployee
{
    public record AlterIdCardEmployeeCommand(Guid EmployeeId, Guid CompanyId, string Cpf, string MotherName, string FatherName, 
        string BirthCity, string BirthState, string Nacionality, DateOnly DateOfBirth) 
        : IRequest<AlterIdCardEmployeeResponse>
    {
        public IdCard ToIdCard() => IdCard.Create(Cpf, MotherName, FatherName, BirthCity, BirthState, Nacionality, DateOfBirth);
    }
}
