namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterWorkPlaceEmployee
{
    public record AlterWorkPlaceEmployeeCommand(Guid EmployeeId, Guid CompanyId, Guid WorkPlaceId) : IRequest<AlterWorkPlaceEmployeeResponse>
    {
    }
}
