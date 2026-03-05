namespace PeopleManagement.Application.Commands.DocumentCommands.GeneratePdfRange
{
    public record GeneratePdfRangeResponseItem(Guid DocumentUnitId, Guid DocumentId, byte[] Pdf);

    public record GeneratePdfRangeResponse(IReadOnlyList<GeneratePdfRangeResponseItem> Results);
}
