namespace PeopleManagement.Application.Commands.RoleCommands.EditRole
{
    public record EditRoleResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator EditRoleResponse(Guid id) =>
            new(id);
    }   
 
}
