namespace PeopleManagement.Application.Commands.DocumentCommands.BatchCreateDocumentUnits
{
    public record BatchCreateDocumentUnitsResponse(IEnumerable<BatchCreatedItem> CreatedItems);

    public record BatchCreatedItem(Guid EmployeeId, Guid DocumentId, Guid DocumentUnitId);
}
