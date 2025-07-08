namespace PeopleManagement.Application.Commands.DepartmentCommands.EditDepartment
{
    public record EditDepartmentResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator EditDepartmentResponse(Guid id) =>
            new(id);
    }
}
