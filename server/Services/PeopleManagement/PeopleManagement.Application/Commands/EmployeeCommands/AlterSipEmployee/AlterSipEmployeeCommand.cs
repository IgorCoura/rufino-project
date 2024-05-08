namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterSipEmployee
{
    public record AlterSipEmployeeCommand(Guid Id, string SipNumber) : IRequest<AlterSipEmployeeResponse>
    {
    }
}
