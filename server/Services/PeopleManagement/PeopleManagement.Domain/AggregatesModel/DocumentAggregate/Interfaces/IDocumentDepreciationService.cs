namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IDocumentDepreciationService
    {
        Task DepreciateExpirateDocument(Guid documentUnitId, Guid documentId, Guid companyId, CancellationToken cancellationToken = default);
        Task WarningExpirateDocument(Guid documentUnitId, Guid documentId, Guid companyId,
            CancellationToken cancellationToken = default);

        // Depreciação disparada pelo início de um novo contrato de trabalho, restrita aos documentos cujo
        // template compõe a NewContractDeprecationPolicy.
        Task DeprecateDocumentsForNewContract(Guid employeeId, Guid companyId,
            CancellationToken cancellationToken = default);
    }
}
