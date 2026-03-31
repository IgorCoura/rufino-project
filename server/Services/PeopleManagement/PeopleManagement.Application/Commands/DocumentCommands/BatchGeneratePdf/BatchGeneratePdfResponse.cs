namespace PeopleManagement.Application.Commands.DocumentCommands.BatchGeneratePdf
{
    public record BatchGeneratePdfResponseItem(
        Guid DocumentUnitId,
        Guid DocumentId,
        string EmployeeName,
        string DocumentName,
        DateOnly DocumentUnitDate,
        byte[] Pdf);

    public record BatchGeneratePdfResponse(IReadOnlyList<BatchGeneratePdfResponseItem> Results);
}
