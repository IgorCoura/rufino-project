namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterWorkPlaceEmployee
{
    public record AlterWorkPlaceEmployeeCommand(Guid EmployeeId, Guid CompanyId, Guid WorkPlaceId) : IRequest<AlterWorkPlaceEmployeeResponse>
    {
    }

    public record AlterWorkPlaceEmployeeModel(Guid EmployeeId, Guid WorkPlaceId)
    {
        public AlterWorkPlaceEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, WorkPlaceId);
    }
}
