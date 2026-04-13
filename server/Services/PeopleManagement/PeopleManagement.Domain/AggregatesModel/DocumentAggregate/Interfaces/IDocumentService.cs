namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentUnit> CreateDocumentUnit(Guid documentId, Guid employeeId, Guid companyId, CancellationToken cancellation = default);
        Task<DocumentUnit> UpdateDocumentUnitDetails(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, DateOnly documentUnitDate, CancellationToken cancellationToken = default);
        Task CreateDocumentUnitsForEvent(Guid employeeId, Guid companyId, int eventId, CancellationToken cancellationToken = default);
        Task<byte[]> GeneratePdf(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, CancellationToken cancellation = default);
        Task<IReadOnlyList<(Guid DocumentUnitId, Guid DocumentId, string DocumentName, DateOnly DocumentUnitDate, byte[] Pdf)>> GeneratePdfRange(
            IEnumerable<(Guid DocumentId, IEnumerable<Guid> DocumentUnitIds)> items,
            Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);
        Task InsertFileWithoutRequireValidation(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, Extension extension, Stream stream, CancellationToken cancellationToken = default);
        Task GenerateDocumentUnitsForRequireDocument(Guid requireDocumentId, Guid companyId, CancellationToken cancellationToken = default);
    }
}
