namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentUnit> CreateDocument(Guid securityDocumentId, Guid employeeId, Guid companyId, DateTime documentDate, CancellationToken cancellation = default);
        Task<byte[]> GeneratePdf(Guid documentId, Guid securityDocumentId, Guid employeeId, Guid companyId, CancellationToken cancellation = default);
        Task InsertFileWithoutRequireValidation(Guid documentId, Guid securityDocumentId, Guid employeeId, Guid companyId, Extension extension, Stream stream, CancellationToken cancellationToken = default);
    }
}
