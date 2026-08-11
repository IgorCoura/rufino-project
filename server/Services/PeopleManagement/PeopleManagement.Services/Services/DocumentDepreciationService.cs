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
        /// <summary>
        /// A unidade chegou na data de validade: fica <c>Vencida</c> — cobertura que caducou e ainda não tem
        /// substituto.
        ///
        /// O job NÃO cria a substituta. Renovar é decisão do RH, feita pela ação "Renovar"
        /// (<see cref="IDocumentService.RenewDocumentUnit"/>): é ela que sabe se ainda há cota de renovação e é
        /// ela que carimba o vínculo entre a substituta e a substituída. Um job criando pendências sozinho
        /// enchia a fila de trabalho com unidades que ninguém pediu.
        /// </summary>
        public async Task DepreciateExpirateDocument(Guid documentUnitId, Guid documentId, Guid companyId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Depreciating document with ID {DocumentId} for company {CompanyId}.", documentId, companyId);

            // Coleção INTEIRA: MakeAsDeprecated varre todas as unidades e RefreshDocumentStatus recalcula o
            // status do documento a partir delas. Com um Include filtrado pela unidade do job, o documento
            // assumia o status de uma unidade só — um job disparando sobre unidade já depreciada deixava o
            // documento inteiro Deprecated, e o funcionário aparecia Okay.
            Document? document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId &&
                x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits),
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
                // Só o vencimento. A unidade que caducou fica Vencida (não Depreciada — ainda não há substituto)
                // e o documento passa a cobrar. Quem decide renovar é o RH.
                if (document.ExpireDocumentUnit(documentUnitId) == false)
                {
                    _logger.LogInformation(
                        "Skipping expiration for document unit {DocumentUnitId} — it is no longer in force.",
                        documentUnitId);
                    return;
                }
            }
            else
            {
                document.MakeAsDeprecated();
            }


            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document with ID {DocumentId} has been marked as expired for company {CompanyId}.", documentId, companyId);
        }


        /// <summary>
        /// A unidade está perto de vencer: entra em <c>A Vencer</c> para o RH providenciar o substituto pela ação
        /// "Renovar". Nada é criado aqui — o aviso é aviso.
        /// </summary>
        public async Task WarningExpirateDocument(Guid documentUnitId, Guid documentId, Guid companyId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Warning expirate document with ID {DocumentId} for company {CompanyId}.", documentId, companyId);

            // Coleção INTEIRA pelo mesmo motivo do vencimento: RefreshDocumentStatus varre DocumentsUnits.
            Document? document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId &&
                x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits),
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
                // Sai sem salvar quando não há transição: o aviso só se aplica a unidade em vigência, e o job
                // pode chegar depois de ela ter sido entregue, superada ou invalidada. Sem essa guarda o
                // RefreshDocumentStatus rodava à toa e o job deixava de ser idempotente.
                if (document.MakeAsWarning(documentUnitId) == false)
                {
                    _logger.LogInformation(
                        "Skipping warning for document unit {DocumentUnitId} — it is no longer in force.",
                        documentUnitId);
                    return;
                }
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

        public async Task<bool> DocumentHasAssociation(Document document, Employee employee, CancellationToken cancellationToken)
        {
            RequireDocuments? reqDocument = await _requireDocumentsRepository.FirstOrDefaultAsync(x => x.Id == document.RequiredDocumentId && x.CompanyId == document.CompanyId, cancellation: cancellationToken);

            if (reqDocument is null)
                return false;

            return reqDocument.AssociationIds.Any(id => employee.IsAssociation(id));
        }
    }
}
