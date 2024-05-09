namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterContactEmployee
{
    public record AlterContactEmployeeCommand(Guid EmployeeId, Guid CompanyId, string CellPhone, string Email) : IRequest<AlterContactEmployeeResponse>
    {
    }
}
