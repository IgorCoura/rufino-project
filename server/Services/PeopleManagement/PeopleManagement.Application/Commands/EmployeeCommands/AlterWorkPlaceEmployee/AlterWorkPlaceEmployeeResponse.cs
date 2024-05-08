namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterWorkPlaceEmployee
{
    public record AlterWorkPlaceEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterWorkPlaceEmployeeResponse(Guid id) => new(id);
    }
}
