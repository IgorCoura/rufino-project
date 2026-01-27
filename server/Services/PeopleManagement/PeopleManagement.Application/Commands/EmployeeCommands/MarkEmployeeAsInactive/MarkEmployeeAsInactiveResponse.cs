namespace PeopleManagement.Application.Commands.EmployeeCommands.MarkEmployeeAsInactive
{
    public record MarkEmployeeAsInactiveResponse(Guid EmployeeId)
    {
        public static implicit operator MarkEmployeeAsInactiveResponse(Guid id) => new(id);
    }
}
