namespace PeopleManagement.Application.Commands.DocumentCommands.DeprecateDocumentUnit
{
    public record DeprecateDocumentUnitResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator DeprecateDocumentUnitResponse(Guid id) => new(id);
    }
}
