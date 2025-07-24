namespace PeopleManagement.Application.Commands.DocumentGroupCommands.CreateDocumentGroup
{
    public record CreateDocumentGroupResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateDocumentGroupResponse(Guid id) =>
            new(id);
    }
}
