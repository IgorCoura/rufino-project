namespace PeopleManagement.Application.Commands.DocumentCommands.MarkAsNotApplicableDocumentUnit
{
    public record MarkAsNotApplicableDocumentUnitResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator MarkAsNotApplicableDocumentUnitResponse(Guid id) => new(id);
    }
}
