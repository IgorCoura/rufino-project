namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces
{
    public interface IRecurringDocumentService
    {
        Task RecurringCreateDocumentUnits(int recurringEventId, CancellationToken cancellationToken = default);
    }
}
