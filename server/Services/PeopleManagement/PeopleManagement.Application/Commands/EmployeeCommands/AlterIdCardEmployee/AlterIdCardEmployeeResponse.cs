namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterIdCardEmployee
{
    public record AlterIdCardEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterIdCardEmployeeResponse(Guid id) => new(id);
    }
}
