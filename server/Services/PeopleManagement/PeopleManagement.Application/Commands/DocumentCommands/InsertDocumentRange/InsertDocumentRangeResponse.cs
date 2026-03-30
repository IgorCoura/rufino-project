namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentRange
{
    public record InsertDocumentRangeResponse(IEnumerable<InsertDocumentRangeResultItem> Results);

    public record InsertDocumentRangeResultItem(
        Guid DocumentUnitId,
        bool Success,
        string? ErrorMessage = null
    );
}
