namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterSocialIntegrationProgramEmployee
{
    public record AlterSocialIntegrationProgramEmployeeCommand(Guid EmployeeId, Guid CompanyId, string SocialIntegrationProgramNumber) : IRequest<AlterSocialIntegrationProgramEmployeeResponse>
    {
    }

    public record AlterSocialIntegrationProgramEmployeeModel(Guid EmployeeId, string SocialIntegrationProgramNumber)
    {
        public AlterSocialIntegrationProgramEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, SocialIntegrationProgramNumber);
    }
}
