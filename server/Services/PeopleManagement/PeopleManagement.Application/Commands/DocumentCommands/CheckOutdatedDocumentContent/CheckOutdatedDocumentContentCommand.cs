namespace PeopleManagement.Application.Commands.DocumentCommands.CheckOutdatedDocumentContent
{
    public record CheckOutdatedDocumentContentCommand(
        IEnumerable<DocumentUnitRef> Items,
        Guid CompanyId
    ) : IRequest<CheckOutdatedDocumentContentResponse>;

    public record CheckOutdatedDocumentContentModel(IEnumerable<DocumentUnitRef> Items)
    {
        public CheckOutdatedDocumentContentCommand ToCommand(Guid company)
            => new(Items, company);
    }
}
