namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.InsertDocumentTemplate
{
    public record InsertDocumentTemplateCommand(Guid DocumentTemplateId, Guid CompanyId, string FileName, Stream Stream) : IRequest<InsertDocumentTemplateResponse>
    {
    }
}
