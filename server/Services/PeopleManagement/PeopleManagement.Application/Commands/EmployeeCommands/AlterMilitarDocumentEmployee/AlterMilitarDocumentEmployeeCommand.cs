namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterMilitarDocumentEmployee
{
    public record AlterMilitarDocumentEmployeeCommand(Guid EmployeeId, Guid CompanyId, string DocumentNumber, string DocumentType) : IRequest<AlterMilitarDocumentEmployeeResponse>
    {
    }
}
