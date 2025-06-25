namespace PeopleManagement.Application.Commands.EmployeeCommands.DocumentSigningOptions
{
    public record DocumentSigningOptionsResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator DocumentSigningOptionsResponse(Guid id) => new(id);
    }
}
