namespace PeopleManagement.Application.Commands.DocumentCommands.MarkAsValidDocumentUnit
{
    public record MarkAsValidDocumentUnitCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId)
        : IRequest<MarkAsValidDocumentUnitResponse>;

    public record MarkAsValidDocumentUnitModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)
    {
        public MarkAsValidDocumentUnitCommand ToCommand(Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company);
    }
}
