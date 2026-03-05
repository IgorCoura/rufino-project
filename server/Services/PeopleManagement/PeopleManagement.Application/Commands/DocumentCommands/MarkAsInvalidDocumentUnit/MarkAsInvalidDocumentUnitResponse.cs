namespace PeopleManagement.Application.Commands.DocumentCommands.MarkAsInvalidDocumentUnit
{
    public record MarkAsInvalidDocumentUnitResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator MarkAsInvalidDocumentUnitResponse(Guid id) => new(id);
    }
}
