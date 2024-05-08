namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterSipEmployee
{
    public record AlterSipEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterSipEmployeeResponse(Guid id) => new(id);
    }
}
