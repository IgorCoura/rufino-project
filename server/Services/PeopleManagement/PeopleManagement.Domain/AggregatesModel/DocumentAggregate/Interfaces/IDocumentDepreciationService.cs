namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IDocumentDepreciationService
    {
        Task DepreciateExpirateDocument(Guid documentUnitId, Guid documentId, Guid companyId, CancellationToken cancellationToken = default);
    }
}
