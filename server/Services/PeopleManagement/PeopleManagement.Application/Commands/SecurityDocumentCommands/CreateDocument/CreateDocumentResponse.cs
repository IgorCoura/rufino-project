namespace PeopleManagement.Application.Commands.SecurityDocumentCommands.CreateDocument
{
    public record CreateDocumentResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateDocumentResponse(Guid id) => new(id);
    }
}
