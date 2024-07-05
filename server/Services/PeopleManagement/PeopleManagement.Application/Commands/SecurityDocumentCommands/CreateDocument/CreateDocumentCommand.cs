namespace PeopleManagement.Application.Commands.SecurityDocumentCommands.CreateDocument
{
    public record CreateDocumentCommand(Guid SecurityDocumentId, Guid EmployeeId, Guid CompanyId, DateTime DocumentDate) : IRequest<CreateDocumentResponse>
    {
    }
}
