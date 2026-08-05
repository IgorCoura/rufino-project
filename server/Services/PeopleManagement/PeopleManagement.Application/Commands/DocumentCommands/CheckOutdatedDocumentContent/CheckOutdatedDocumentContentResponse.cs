namespace PeopleManagement.Application.Commands.DocumentCommands.CheckOutdatedDocumentContent
{
    public record OutdatedDocumentContentItem(Guid DocumentUnitId, bool IsOutdated, bool CheckFailed);

    public record CheckOutdatedDocumentContentResponse(IReadOnlyList<OutdatedDocumentContentItem> Items);
}
