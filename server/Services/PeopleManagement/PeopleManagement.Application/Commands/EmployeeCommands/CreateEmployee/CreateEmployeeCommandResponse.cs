namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee
{
    public record CreateEmployeeCommandResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateEmployeeCommandResponse(Guid id) => new(id);  
    }
}
