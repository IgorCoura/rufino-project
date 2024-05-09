namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterSipEmployee
{
    public record AlterSipEmployeeCommand(Guid EmployeeId, Guid CompanyId, string SipNumber) : IRequest<AlterSipEmployeeResponse>
    {
    }
}
