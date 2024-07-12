namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate
{
    public record CreateDocumentTemplateResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateDocumentTemplateResponse(Guid id) => new(id);
    }
}
