namespace PeopleManagement.Application.Commands.RoleCommands.CreateRole
{
    public record CreateRoleResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateRoleResponse(Guid id) =>
            new(id);
    }
}
