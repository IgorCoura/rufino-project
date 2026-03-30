namespace PeopleManagement.Application.Commands.DocumentCommands.BatchCreateDocumentUnits
{
    public record BatchCreateDocumentUnitsCommand(
        Guid DocumentTemplateId,
        IEnumerable<Guid> EmployeeIds,
        Guid CompanyId
    ) : IRequest<BatchCreateDocumentUnitsResponse>;

    public record BatchCreateDocumentUnitsModel(
        Guid DocumentTemplateId,
        IEnumerable<Guid> EmployeeIds)
    {
        public BatchCreateDocumentUnitsCommand ToCommand(Guid company)
            => new(DocumentTemplateId, EmployeeIds, company);
    }
}
