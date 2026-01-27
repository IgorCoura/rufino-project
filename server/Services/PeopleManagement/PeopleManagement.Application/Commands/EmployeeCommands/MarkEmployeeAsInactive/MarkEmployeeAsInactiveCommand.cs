namespace PeopleManagement.Application.Commands.EmployeeCommands.MarkEmployeeAsInactive
{
    public record MarkEmployeeAsInactiveCommand(Guid EmployeeId, Guid CompanyId) : IRequest<MarkEmployeeAsInactiveResponse>
    {
    }

    public record MarkEmployeeAsInactiveModel(Guid EmployeeId)
    {
        public MarkEmployeeAsInactiveCommand ToCommand(Guid company) => new(EmployeeId, company);
    }
}
