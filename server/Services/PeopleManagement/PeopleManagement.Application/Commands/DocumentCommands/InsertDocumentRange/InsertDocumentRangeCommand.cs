namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentRange
{
    public record InsertDocumentRangeCommand(
        IEnumerable<InsertDocumentRangeItem> Items,
        Guid CompanyId
    ) : IRequest<InsertDocumentRangeResponse>;

    public record InsertDocumentRangeItem(
        Guid DocumentUnitId,
        Guid DocumentId,
        Guid EmployeeId,
        string Extension,
        Stream Stream
    );
}
