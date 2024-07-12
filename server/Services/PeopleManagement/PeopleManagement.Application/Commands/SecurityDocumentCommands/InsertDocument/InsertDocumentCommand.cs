namespace PeopleManagement.Application.Commands.SecurityDocumentCommands.InsertDocument
{
    public record InsertDocumentCommand(Guid DocumentId, Guid SecurityDocumentId, Guid EmployeeId, 
        Guid CompanyId, string Extension, Stream Stream) : IRequest<InsertDocumentResponse>
    {
    }
}
