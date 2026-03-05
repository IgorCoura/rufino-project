namespace PeopleManagement.Application.Commands.DocumentCommands.MarkAsInvalidDocumentUnit
{
    public record MarkAsInvalidDocumentUnitCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId)
        : IRequest<MarkAsInvalidDocumentUnitResponse>;

    public record MarkAsInvalidDocumentUnitModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)
    {
        public MarkAsInvalidDocumentUnitCommand ToCommand(Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company);
    }
}
