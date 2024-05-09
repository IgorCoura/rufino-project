namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterVoteIdEmployee
{
    public record AlterVoteIdEmployeeCommand(Guid EmployeeId, Guid CompanyId, string VoteIdNumber) : IRequest<AlterVoteIdEmployeeResponse>
    {
    }
}
