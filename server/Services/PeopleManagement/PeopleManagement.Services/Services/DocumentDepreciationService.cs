using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace PeopleManagement.Services.Services
{
    public class DocumentDepreciationService(IDocumentRepository documentRepository, ILogger<DocumentDepreciationService> logger) : IDocumentDepreciationService
    {
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly ILogger<DocumentDepreciationService> _logger = logger;
        public async Task DepreciateExpirateDocument(Guid documentUnitId, Guid documentId, Guid companyId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Depreciating document with ID {DocumentId} for company {CompanyId}.", documentId, companyId);

            Document? document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId &&
                x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits.Where(x => x.Id == documentUnitId)),
                cancellation: cancellationToken);

            if (document is null)
            {
                _logger.LogError("Document with ID {DocumentId} not found for company {CompanyId}.", documentId, companyId);
                return;
            }

            var newDocumentUnitId = Guid.NewGuid();
            document.MakeAsDocumentExpired(documentUnitId, newDocumentUnitId);

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document with ID {DocumentId} has been marked as expired for company {CompanyId}.", documentId, companyId);
        }


        public async Task WarningExpirateDocument(Guid documentUnitId, Guid documentId, Guid companyId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Warning expirate document with ID {DocumentId} for company {CompanyId}.", documentId, companyId);

            Document? document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId &&
                x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits.Where(x => x.Id == documentUnitId)),
                cancellation: cancellationToken);

            if (document is null)
            {
                _logger.LogError("Document with ID {DocumentId} not found for company {CompanyId}.", documentId, companyId);
                return;
            }

            var newDocumentUnitId = Guid.NewGuid();
            document.MakeAsWarning(documentUnitId, newDocumentUnitId);

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document with ID {DocumentId} has been marked as warning for company {CompanyId}.", documentId, companyId);
        }

    }
}
