namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces
{
    public interface ISecurityDocumentService
    {
        Task<Document> CreateDocument(Guid securityDocumentId, Guid employeeId, Guid companyId, DateTime documentDate, CancellationToken cancellation = default);
        Task<byte[]> GeneratePdf(Guid documentId, Guid securityDocumentId, Guid employeeId, Guid companyId, CancellationToken cancellation = default);
    }
}
