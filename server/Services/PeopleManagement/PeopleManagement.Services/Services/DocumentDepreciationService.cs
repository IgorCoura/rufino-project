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
                // no Document. Lê a policy do template e o contador de vencimentos do documento; a unidade que
                // venceu fica Vencida (não Depreciada — ainda não há substituto), mas só nasce uma nova enquanto a
                // policy permitir renovar. Sem policy de vencimento (documento legado com data de validade avulsa)
                // mantém o comportamento antigo: renova sempre.
                var template = await _documentTemplateRepository.FirstOrDefaultAsync(
                    x => x.Id == document.DocumentTemplateId && x.CompanyId == companyId,
                    cancellation: cancellationToken);

                // ANTES de expirar: CanRenew recebe quantos vencimentos já tinham acontecido, então o vencimento
                // atual é justamente o que está pedindo a renovação de número ExpirationCount + 1.
                var canRenew = CanRenew(document, template);

                document.ExpireDocumentUnit(documentUnitId);

                if (canRenew)
                {
                    // A renovada nasce sem data de referência: se o template for por competência, cai na mínima e
                    // espera a data real. A configuração é a ATUAL do template — editar o template vale para a
                    // renovação seguinte.
                    var periodPolicy = template?.GetPolicy<IPeriodPolicy>();
                    document.NewDocumentUnit(Guid.NewGuid(), periodPolicy?.PeriodType, periodPolicy?.UsePreviousPeriod ?? false);
                }
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

        /// <summary>
        /// Deprecia as unidades entregues (OK) do funcionário quando um novo contrato de trabalho começa,
        /// restrito aos documentos cujo template compõe a <see cref="INewContractDeprecationPolicy"/>.
        ///
        /// Mora aqui, e não no Document, porque a regra cruza dois aggregates: quem decide é o template, quem
        /// muda é o documento. A policy é lida AO VIVO — editar o template vale para a próxima admissão.
        ///
        /// Sem SaveChanges: roda dentro do despacho de eventos de domínio, que acontece antes do SaveChanges do
        /// UnitOfWork que o disparou — salvar aqui re-despacharia os eventos ainda na fila.
        /// </summary>
        public async Task DeprecateDocumentsForNewContract(Guid employeeId, Guid companyId,
            CancellationToken cancellationToken = default)
        {
            // Include de TODAS as unidades: DeprecateDeliveredUnits recalcula o status do documento varrendo a
            // coleção, e com uma coleção parcial ele mentiria.
            var documents = await _documentRepository.GetDataAsync(
                x => x.EmployeeId == employeeId && x.CompanyId == companyId,
                include: i => i.Include(x => x.DocumentsUnits),
                cancellation: cancellationToken);

            var documentList = documents.ToList();

            if (documentList.Count == 0)
                return;

            var templateIds = documentList.Select(x => x.DocumentTemplateId).Distinct().ToList();

            var templatesById = (await _documentTemplateRepository.GetDataAsync(
                x => templateIds.Contains(x.Id) && x.CompanyId == companyId,
                cancellation: cancellationToken)).ToDictionary(x => x.Id);

            var deprecatedUnits = 0;

            foreach (var document in documentList)
            {
                if (!templatesById.TryGetValue(document.DocumentTemplateId, out var template))
                {
                    _logger.LogWarning("Document template {TemplateId} not found for company {CompanyId}. Skipping.",
                        document.DocumentTemplateId, companyId);
                    continue;
                }

                if (template.HasPolicy<INewContractDeprecationPolicy>() == false)
                    continue;

                deprecatedUnits += document.DeprecateDeliveredUnits();
            }

            _logger.LogInformation(
                "Deprecated {UnitCount} document unit(s) for employee {EmployeeId} of company {CompanyId} on new contract.",
                deprecatedUnits, employeeId, companyId);
        }

        // Consulta a policy de vencimento do template e decide, pelo contador de vencimentos do documento, se
        // ainda pode renovar. Sem policy ⇒ renova sempre (retrocompatível).
        //
        // O contador é Document.ExpirationCount, e não uma contagem de unidades por status: a vencida vira
        // Depreciada quando o substituto chega, e substituição por reenvio corrigido também deprecia — contar
        // status fazia correção de documento consumir renovação.
        private static bool CanRenew(Document document,
            Domain.AggregatesModel.DocumentTemplateAggregate.DocumentTemplate? template)
        {
            var expirationPolicy = template?.GetPolicy<IExpirationPolicy>();

            if (expirationPolicy is null)
                return true;

            return expirationPolicy.CanRenew(document.ExpirationCount);
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
