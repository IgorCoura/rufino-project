namespace PeopleManagement.Application.Commands.EmployeeCommands.CompleteAdmissionEmployee
{
    public record CompleteAdmissionEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CompleteAdmissionEmployeeResponse(Guid id) => new(id);
    }
}
