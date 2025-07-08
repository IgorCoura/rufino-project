namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterContactEmployee
{
    public record AlterContactEmployeeResponse(Guid Id) : BaseDTO(Id) 
    {
        public static implicit operator AlterContactEmployeeResponse(Guid id) => new(id);
    }
}
