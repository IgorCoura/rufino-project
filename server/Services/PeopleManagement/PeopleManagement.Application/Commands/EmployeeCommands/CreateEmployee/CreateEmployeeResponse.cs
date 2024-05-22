namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee
{
    public record CreateEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateEmployeeResponse(Guid id) => new(id);  
    }
}
