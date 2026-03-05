namespace PeopleManagement.Application.Commands.DocumentCommands.MarkAsNotApplicableDocumentUnit
{
    public record MarkAsNotApplicableDocumentUnitCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId)
        : IRequest<MarkAsNotApplicableDocumentUnitResponse>;

    public record MarkAsNotApplicableDocumentUnitModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)
    {
        public MarkAsNotApplicableDocumentUnitCommand ToCommand(Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company);
    }
}
