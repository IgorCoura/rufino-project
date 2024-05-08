namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterContactEmployee
{
    public record AlterContactEmployeeCommand(Guid Id, string CellPhone, string Email) : IRequest<AlterContactEmployeeResponse>
    {
    }
}
