namespace PeopleManagement.Application.Commands.DocumentCommands.BatchUpdateDocumentUnitDate
{
    public record BatchUpdateDocumentUnitDateCommand(
        IEnumerable<BatchUpdateDateItem> Items,
        DateOnly Date,
        Guid CompanyId
    ) : IRequest<BatchUpdateDocumentUnitDateResponse>;

    public record BatchUpdateDateItem(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId);

    public record BatchUpdateDocumentUnitDateModel(
        IEnumerable<BatchUpdateDateItem> Items,
        DateOnly Date)
    {
        public BatchUpdateDocumentUnitDateCommand ToCommand(Guid company)
            => new(Items, Date, company);
    }
}
