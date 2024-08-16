namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocument
{
    public record InsertDocumentCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, 
        Guid CompanyId, string Extension, Stream Stream) : IRequest<InsertDocumentResponse>
    {
    }
}
