namespace PeopleManagement.Application.Commands.DocumentCommands.MarkAsValidDocumentUnit
{
    public record MarkAsValidDocumentUnitResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator MarkAsValidDocumentUnitResponse(Guid id) => new(id);
    }
}
