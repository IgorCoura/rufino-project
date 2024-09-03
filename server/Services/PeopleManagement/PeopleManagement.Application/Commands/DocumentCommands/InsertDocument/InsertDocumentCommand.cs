namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocument
{
    public record InsertDocumentCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, 
        Guid CompanyId, string Extension, Stream Stream) : IRequest<InsertDocumentResponse>
    {
    }

    public record InsertDocumentModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)
    {
        public InsertDocumentCommand ToCommand(Guid company, string extension, Stream stream) 
            => new(DocumentUnitId, DocumentId, EmployeeId, company, extension, stream);
    }
}
