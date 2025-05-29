using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;

namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces
{
    public interface IRecurringDocumentService
    {
        Task RecurringCreateDocumentUnits(RecurringEvents recurringEvent, CancellationToken cancellationToken = default);
    }
}
