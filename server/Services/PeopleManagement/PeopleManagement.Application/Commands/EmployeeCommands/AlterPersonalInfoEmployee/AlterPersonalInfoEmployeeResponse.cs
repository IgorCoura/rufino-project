namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterPersonalInfoEmployee
{
    public record AlterPersonalInfoEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterPersonalInfoEmployeeResponse(Guid id) => new(id);
    }
}
