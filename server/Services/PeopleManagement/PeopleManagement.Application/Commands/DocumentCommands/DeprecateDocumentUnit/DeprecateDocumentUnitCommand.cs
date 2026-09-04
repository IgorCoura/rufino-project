namespace PeopleManagement.Application.Commands.DocumentCommands.DeprecateDocumentUnit
{
    public record DeprecateDocumentUnitCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId)
        : IRequest<DeprecateDocumentUnitResponse>;

    public record DeprecateDocumentUnitModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)
    {
        public DeprecateDocumentUnitCommand ToCommand(Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company);
    }
}
