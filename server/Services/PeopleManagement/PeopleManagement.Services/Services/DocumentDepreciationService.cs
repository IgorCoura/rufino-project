using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using System.Threading;


namespace PeopleManagement.Services.Services
{
    public class DocumentDepreciationService(ILogger<DocumentDepreciationService> logger, IDocumentRepository documentRepository, 
        IRequireDocumentsRepository requireDocumentsRepository, IEmployeeRepository employeeRepository) : IDocumentDepreciationService
    {
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly ILogger<DocumentDepreciationService> _logger = logger;
        private readonly IRequireDocumentsRepository _requireDocumentsRepository = requireDocumentsRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
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

           

            var isAssociation = await DocumentHasAssociation(document, cancellationToken);

            if (isAssociation)
            {
                var newDocumentUnitId = Guid.NewGuid();
                document.MakeAsDocumentDeprecated(documentUnitId, newDocumentUnitId);
                document.NewDocumentUnit(Guid.NewGuid());
            }
            else
            {
                document.MakeAsDeprecated();
            }


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

            var isAssociation = await DocumentHasAssociation(document, cancellationToken);

            if (isAssociation)
            {
                document.MakeAsWarning(documentUnitId);
            }
            else
            {
                document.MakeAsDeprecated();
            }


            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document with ID {DocumentId} has been marked as warning for company {CompanyId}.", documentId, companyId);
        }

        public async Task<bool> DocumentHasAssociation(Document document, CancellationToken cancellationToken)
        {
            RequireDocuments? reqDocument = await _requireDocumentsRepository.FirstOrDefaultAsync(x => x.Id == document.RequiredDocumentId && x.CompanyId == document.CompanyId, cancellation: cancellationToken);
            Employee? employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == document.EmployeeId && x.CompanyId == document.CompanyId, cancellation: cancellationToken);

            if (employee is not null && reqDocument is not null)
            {
                return employee.IsAssociation(reqDocument.AssociationId);
            }
            return false;
        }
    }
}
