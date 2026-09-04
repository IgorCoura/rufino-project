namespace PeopleManagement.Application.Commands.DocumentCommands.RenewDocumentUnit
{
    public record RenewDocumentUnitCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId)
        : IRequest<RenewDocumentUnitResponse>;

    public record RenewDocumentUnitModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)
    {
        public RenewDocumentUnitCommand ToCommand(Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company);
    }
}
