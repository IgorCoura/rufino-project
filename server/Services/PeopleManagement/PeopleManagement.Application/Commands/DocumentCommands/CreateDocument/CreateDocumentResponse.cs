namespace PeopleManagement.Application.Commands.DocumentCommands.CreateDocument
{
    public record CreateDocumentResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateDocumentResponse(Guid id) => new(id);
    }
}
