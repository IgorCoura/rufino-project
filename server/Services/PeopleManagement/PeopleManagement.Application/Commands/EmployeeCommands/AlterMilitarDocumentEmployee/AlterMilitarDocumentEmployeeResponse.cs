namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterMilitarDocumentEmployee
{
    public record AlterMilitarDocumentEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterMilitarDocumentEmployeeResponse(Guid id) => new(id);
    }
}
