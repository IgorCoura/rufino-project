namespace PeopleManagement.Application.Commands.DocumentCommands.CreateDocument
{
    public record CreateDocumentCommand(Guid DocumentId, Guid EmployeeId, Guid CompanyId, DateTime DocumentDate) : IRequest<CreateDocumentResponse>
    {
    }
}
