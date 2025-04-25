namespace PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails
{
    public record UpdateDocumentUnitDetailsResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator UpdateDocumentUnitDetailsResponse(Guid id) => new(id);
    }
}
