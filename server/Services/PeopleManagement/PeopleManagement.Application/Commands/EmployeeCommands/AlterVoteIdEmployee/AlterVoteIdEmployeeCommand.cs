namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterVoteIdEmployee
{
    public record AlterVoteIdEmployeeCommand(Guid EmployeeId, Guid CompanyId, string VoteIdNumber) : IRequest<AlterVoteIdEmployeeResponse>
    {
    }

    public record AlterVoteIdEmployeeModel(Guid EmployeeId, string VoteIdNumber)
    {
        public AlterVoteIdEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, VoteIdNumber);
    }
}
