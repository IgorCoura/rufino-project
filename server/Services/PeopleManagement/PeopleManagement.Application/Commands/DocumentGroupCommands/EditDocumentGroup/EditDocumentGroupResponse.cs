namespace PeopleManagement.Application.Commands.DocumentGroupCommands.EditDocumentGroup
{
    public record EditDocumentGroupResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator EditDocumentGroupResponse(Guid id) =>
            new(id);
    }
}
