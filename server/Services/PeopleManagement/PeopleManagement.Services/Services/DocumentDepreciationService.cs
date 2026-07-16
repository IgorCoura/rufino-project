using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using System.Threading;


namespace PeopleManagement.Services.Services
{
    public class DocumentDepreciationService(ILogger<DocumentDepreciationService> logger, IDocumentRepository documentRepository,
        IRequireDocumentsRepository requireDocumentsRepository, IEmployeeRepository employeeRepository,
        IDocumentTemplateRepository documentTemplateRepository) : IDocumentDepreciationService
    {
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly ILogger<DocumentDepreciationService> _logger = logger;
        private readonly IRequireDocumentsRepository _requireDocumentsRepository = requireDocumentsRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;
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

            Employee? employee = await _employeeRepository.FirstOrDefaultAsync(
                x => x.Id == document.EmployeeId && x.CompanyId == document.CompanyId,
                cancellation: cancellationToken);

            if (employee is null || employee.Status == Status.Inactive)
            {
                _logger.LogInformation(
                    "Skipping depreciation for document {DocumentId} — employee {EmployeeId} is inactive or missing.",
                    documentId, document.EmployeeId);
                return;
            }

            var isAssociation = await DocumentHasAssociation(document, employee, cancellationToken);

            if (isAssociation)
            {
                // Renovação limitada é regra de dois aggregates (Document + DocumentTemplate), logo mora aqui, não
                // no Document. Lê a policy do template e o contador de renovações (unidades já depreciadas); a
                // unidade que venceu é sempre depreciada, mas só nasce uma nova enquanto a policy permitir renovar.
                // Sem policy de vencimento (documento legado com data de validade avulsa) mantém o comportamento
                // antigo: renova sempre.
                var canRenew = await CanRenewAsync(document, companyId, cancellationToken);

                var newDocumentUnitId = Guid.NewGuid();
                document.MakeAsDocumentDeprecated(documentUnitId, newDocumentUnitId);

                if (canRenew)
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

            Employee? employee = await _employeeRepository.FirstOrDefaultAsync(
                x => x.Id == document.EmployeeId && x.CompanyId == document.CompanyId,
                cancellation: cancellationToken);

            if (employee is null || employee.Status == Status.Inactive)
            {
                _logger.LogInformation(
                    "Skipping warning for document {DocumentId} — employee {EmployeeId} is inactive or missing.",
                    documentId, document.EmployeeId);
                return;
            }

            var isAssociation = await DocumentHasAssociation(document, employee, cancellationToken);

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

        // Consulta a policy de vencimento do template do documento e decide, pelo contador de renovações
        // (unidades depreciadas), se ainda pode renovar. Sem policy ⇒ renova sempre (retrocompatível).
        private async Task<bool> CanRenewAsync(Document document, Guid companyId, CancellationToken cancellationToken)
        {
            var template = await _documentTemplateRepository.FirstOrDefaultAsync(
                x => x.Id == document.DocumentTemplateId && x.CompanyId == companyId,
                cancellation: cancellationToken);

            var expirationPolicy = template?.GetPolicy<IExpirationPolicy>();
            if (expirationPolicy is null)
                return true;

            var renewalCount = await _documentRepository.CountDeprecatedUnitsAsync(document.Id, companyId, cancellationToken);
            return expirationPolicy.CanRenew(renewalCount);
        }

        public async Task<bool> DocumentHasAssociation(Document document, Employee employee, CancellationToken cancellationToken)
        {
            RequireDocuments? reqDocument = await _requireDocumentsRepository.FirstOrDefaultAsync(x => x.Id == document.RequiredDocumentId && x.CompanyId == document.CompanyId, cancellation: cancellationToken);

            if (reqDocument is null)
                return false;

            return reqDocument.AssociationIds.Any(id => employee.IsAssociation(id));
        }
    }
}
