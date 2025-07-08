namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.EditDocumentTemplate
{
   
    public record EditDocumentTemplateResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator EditDocumentTemplateResponse(Guid id) => new(id);
    }
}
