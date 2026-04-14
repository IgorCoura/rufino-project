namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterSocialIntegrationProgramEmployee
{
    public record AlterSocialIntegrationProgramEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterSocialIntegrationProgramEmployeeResponse(Guid id) => new(id);
    }
}
