namespace PeopleManagement.Application.Commands.DocumentCommands.CreateDocument
{
    public record CreateDocumentCommand(Guid DocumentId, Guid EmployeeId, Guid CompanyId) : IRequest<CreateDocumentResponse>
    {
    }

    public record CreateDocumentModel(Guid DocumentId, Guid EmployeeId)
    {
        public CreateDocumentCommand ToCommand(Guid company) => new(DocumentId, EmployeeId, company);
    }
}
