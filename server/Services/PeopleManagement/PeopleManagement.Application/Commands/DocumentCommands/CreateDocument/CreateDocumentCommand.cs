namespace PeopleManagement.Application.Commands.DocumentCommands.CreateDocument
{
    public record CreateDocumentCommand(Guid DocumentId, Guid EmployeeId, Guid CompanyId, DateTime DocumentDate) : IRequest<CreateDocumentResponse>
    {
    }

    public record CreateDocumentModel(Guid DocumentId, Guid EmployeeId, DateTime DocumentDate)
    {
        public CreateDocumentCommand ToCommand(Guid company) => new(DocumentId, EmployeeId, company, DocumentDate);
    }
}
