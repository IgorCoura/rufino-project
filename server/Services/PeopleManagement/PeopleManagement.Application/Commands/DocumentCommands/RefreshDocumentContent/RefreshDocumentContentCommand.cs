namespace PeopleManagement.Application.Commands.DocumentCommands.RefreshDocumentContent
{
    public record RefreshDocumentContentCommand(
        IEnumerable<DocumentUnitRef> Items,
        Guid CompanyId
    ) : IRequest<RefreshDocumentContentResponse>;

    public record RefreshDocumentContentModel(IEnumerable<DocumentUnitRef> Items)
    {
        public RefreshDocumentContentCommand ToCommand(Guid company)
            => new(Items, company);
    }
}
