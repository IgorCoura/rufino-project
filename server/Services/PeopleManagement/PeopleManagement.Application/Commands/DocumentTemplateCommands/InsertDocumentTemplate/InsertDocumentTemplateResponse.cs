namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.InsertDocumentTemplate
{
    public record InsertDocumentTemplateResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator InsertDocumentTemplateResponse(Guid id) => new(id);
    }
}
