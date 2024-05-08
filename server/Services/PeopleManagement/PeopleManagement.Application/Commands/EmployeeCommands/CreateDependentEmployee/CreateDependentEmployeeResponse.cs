namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateDependentEmployee
{
    public record CreateDependentEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateDependentEmployeeResponse(Guid id) => new(id);
    }
}
