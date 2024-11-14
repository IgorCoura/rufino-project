namespace PeopleManagement.Application.Commands.EmployeeCommands.IsRequiredMilitaryDocumentEmployee
{
    public record IsRequiredMilitaryDocumentEmployeeResponse(Guid Id, bool IsRequired) : BaseDTO(Id)
    {
        public static implicit operator IsRequiredMilitaryDocumentEmployeeResponse(Guid id) => new(id);
    }
}
