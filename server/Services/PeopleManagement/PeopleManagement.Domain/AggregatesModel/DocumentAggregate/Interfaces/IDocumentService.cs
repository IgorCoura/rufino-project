namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentUnit> CreateDocumentUnit(Guid documentId, Guid employeeId, Guid companyId, DateTime documentUnitDate, CancellationToken cancellation = default);
        Task<byte[]> GeneratePdf(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, CancellationToken cancellation = default);
        Task InsertFileWithoutRequireValidation(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, Extension extension, Stream stream, CancellationToken cancellationToken = default);
    }
}
