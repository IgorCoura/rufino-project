namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces
{
    public interface IRecoverInfoToDocumentTemplateService
    {
        Task<string> RecoverInfo(Guid id, Guid companyId, DateOnly date, CancellationToken cancellation = default);
    }
}
