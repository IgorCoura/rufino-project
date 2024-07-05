namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces
{
    public interface IRecoverInfoToSecurityDocumentService
    {
        Task<string> RecoverInfo(Guid id, Guid companyId, DateTime date, CancellationToken cancellation = default);
    }
}
