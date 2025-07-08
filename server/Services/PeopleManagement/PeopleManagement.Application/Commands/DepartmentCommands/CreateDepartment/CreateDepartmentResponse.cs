namespace PeopleManagement.Application.Commands.DepartmentCommands.CreateDepartment
{
    public record CreateDepartmentResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateDepartmentResponse(Guid id) =>
            new(id);
    }
}
