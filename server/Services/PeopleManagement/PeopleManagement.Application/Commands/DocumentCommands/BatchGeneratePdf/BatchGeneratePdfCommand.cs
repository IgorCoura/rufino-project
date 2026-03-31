namespace PeopleManagement.Application.Commands.DocumentCommands.BatchGeneratePdf
{
    public record BatchGeneratePdfItem(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId);

    public record BatchGeneratePdfCommand(
        IEnumerable<BatchGeneratePdfItem> Items,
        Guid CompanyId
    ) : IRequest<BatchGeneratePdfResponse>;

    public record BatchGeneratePdfModel(IEnumerable<BatchGeneratePdfItem> Items)
    {
        public BatchGeneratePdfCommand ToCommand(Guid company) => new(Items, company);
    }
}
