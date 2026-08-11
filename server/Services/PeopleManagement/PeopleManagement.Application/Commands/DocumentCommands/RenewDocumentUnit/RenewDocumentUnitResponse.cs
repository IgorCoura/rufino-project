namespace PeopleManagement.Application.Commands.DocumentCommands.RenewDocumentUnit
{
    public record RenewDocumentUnitResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator RenewDocumentUnitResponse(Guid id) => new(id);
    }
}
