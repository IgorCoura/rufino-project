namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterVoteIdEmployee
{
    public record AlterVoteIdEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterVoteIdEmployeeResponse(Guid id) => new(id);
    }
}
