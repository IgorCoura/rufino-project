namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterWorkPlaceEmployee
{
    public record AlterWorkPlaceEmployeeCommand(Guid Id, Guid WorkPlaceId) : IRequest<AlterWorkPlaceEmployeeResponse>
    {
    }
}
