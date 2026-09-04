using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.WorkloadCalendar;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.Options;
using PeopleManagement.Domain.Utils;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json.Nodes;
using System.Threading;
using Document = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Document;
using Employee = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Employee;
using Extension = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Extension;

namespace PeopleManagement.Services.Services
{
    public class DocumentService(IDocumentRepository securityDocumentRepository, IServiceProvider serviceProvider,
        IPdfService pdfService, IBlobService blobService, IDocumentTemplateRepository documentTemplateRepository,
        DocumentTemplatesOptions documentTemplatesOptions, IRequireDocumentsRepository requireDocumentsRepository,
        IEmployeeRepository employeeRepository, IOptions<TimeZoneOptions> timeZoneOptions, ILogger<DocumentService> logger,
        IWorkloadCalendarService workloadCalendarService, IDocumentContentBuilder documentContentBuilder) : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository = securityDocumentRepository;
        private readonly IPdfService _pdfService = pdfService;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IBlobService _blobService = blobService;
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;
        private readonly DocumentTemplatesOptions _documentTemplatesOptions = documentTemplatesOptions;
        private readonly IRequireDocumentsRepository _requireDocumentsRepository = requireDocumentsRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly ILogger<DocumentService> _logger = logger;
        private readonly TimeZoneOptions _timeZone = timeZoneOptions.Value;
        private readonly IWorkloadCalendarService _workloadCalendarService = workloadCalendarService;
        private readonly IDocumentContentBuilder _documentContentBuilder = documentContentBuilder;

        // Criar com data é o RH preenchendo uma competência que ficou sem unidade. Só documento por competência
        // aceita: nos demais duas unidades não podem cobrir ao mesmo tempo, e é por isso que a próxima nasce de
        // depreciar/invalidar a vigente ou de renovar. A guarda de competência ocupada é do agregado
        // (NewDocumentUnitForPeriod) — aqui mora só o que depende do template, que é outro aggregate.
        //
        // Sem data o caminho é o de antes, intacto, porque o app legado ainda cria unidade assim.
        public async Task<DocumentUnit> CreateDocumentUnit(Guid documentId, Guid employeeId, Guid companyId,
            DateOnly? date = null, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId
                && x.CompanyId == companyId, include: i => i.Include(x => x.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(
                x => x.Id == document.DocumentTemplateId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            var (usePreviousPeriod, periodType) = PeriodConfigOf(documentTemplate);
            var documentUnitId = Guid.NewGuid();

            if (date is null)
                return document.NewDocumentUnit(documentUnitId, periodType, usePreviousPeriod);

            if (periodType is null)
                throw new DomainException(this, DomainErrors.Document.DocumentIsNotPeriodic(documentId));

            var documentUnit = document.NewDocumentUnitForPeriod(documentUnitId, periodType, usePreviousPeriod,
                date.Value.ToDateTime(TimeOnly.MinValue));

            // Preenche já: sem isso a unidade nasceria com a data mas sem validade nem snapshot, e o RH teria de
            // digitar a mesma data de novo em "editar data" antes de conseguir gerar o documento.
            return await FillUnitDetails(document, documentTemplate, documentUnit.Id, date.Value, employeeId, companyId,
                cancellationToken);
        }

        public Task<DocumentUnit> DeprecateDocumentUnit(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId,
            CancellationToken cancellationToken = default)
            => ReplaceDocumentUnit(documentUnitId, documentId, employeeId, companyId,
                (document, unitId) => document.DeprecateDocumentUnit(unitId), cancellationToken);

        public Task<DocumentUnit> InvalidateDocumentUnit(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId,
            CancellationToken cancellationToken = default)
            => ReplaceDocumentUnit(documentUnitId, documentId, employeeId, companyId,
                (document, unitId) => document.MarkAsInvalidDocumentUnit(unitId), cancellationToken);

        // Tira a unidade de cena e devolve a pendente que fica no lugar. A criação da substituta precisa da
        // configuração de competência do template — regra de outro aggregate — então mora aqui, e para o
        // documento desce só o valor.
        //
        // NewDocumentUnit reaproveita uma pendente equivalente quando já existe, então chamar sempre é seguro:
        // depreciar duas unidades da mesma competência não gera duas pendências.
        private async Task<DocumentUnit> ReplaceDocumentUnit(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId,
            Action<Document, Guid> removeFromService, CancellationToken cancellationToken)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId
                && x.CompanyId == companyId, include: i => i.Include(x => x.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(
                x => x.Id == document.DocumentTemplateId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            removeFromService(document, documentUnitId);

            var (usePreviousPeriod, periodType) = PeriodConfigOf(documentTemplate);

            return document.NewDocumentUnit(Guid.NewGuid(), periodType, usePreviousPeriod);
        }

        // Renovar cruza dois aggregates — a cota de ciclos de validade é regra do template, o contador é do
        // documento —, então a decisão mora aqui e para o agregado desce só o fato.
        //
        // Renovar NUNCA é recusado por causa do teto. O teto diz quantas vezes o documento vence, não quantas
        // vezes o RH pode agir: esgotado, a substituta continua sendo criada, só que sem validade (ver
        // ValidityDurationFor) — e por isso não consome ciclo. Enquanto o teto recusava a renovação, uma unidade
        // vencida com a cota esgotada ficava sem saída nenhuma na tela: não dava para renovar, e vencida também
        // não é invalidável (é a prova do período coberto). Chegava-se lá por dado legado (o contador foi
        // backfillado de vencimentos), por edição do teto no template, ou simplesmente descartando a substituta
        // de uma renovação já feita.
        //
        // A ordem importa: idempotência primeiro. Um pedido repetido (duplo clique, retry de rede que passou pelo
        // IdentifiedCommand) precisa devolver a mesma substituta em vez de consumir um segundo ciclo.
        public async Task<DocumentUnit> RenewDocumentUnit(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId,
            CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId
                && x.CompanyId == companyId, include: i => i.Include(x => x.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(
                x => x.Id == document.DocumentTemplateId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            var liveReplacement = document.LiveReplacementFor(documentUnitId);
            if (liveReplacement is not null)
                return liveReplacement;

            // Esta renovação abre um ciclo de validade novo? Sem policy de vencimento, nunca houve ciclo a gastar
            // — é o documento com validade avulsa. Com o teto esgotado, a substituta vai nascer sem validade, e o
            // que não vence não consome cota: é o que mantém o contador significando "ciclos gastos" e faz
            // aumentar o teto no template devolver ciclos de verdade.
            var expirationPolicy = documentTemplate.GetPolicy<IExpirationPolicy>();
            var opensNewValidityCycle = expirationPolicy is not null &&
                expirationPolicy.HasValidityCycleLeft(document.RenewalCount);

            // Sem data de referência: se o template for por competência, a substituta cai na mínima e espera a
            // data real. A configuração é a ATUAL do template — editar o template vale da próxima renovação em
            // diante.
            var (usePreviousPeriod, periodType) = PeriodConfigOf(documentTemplate);
            var replacement = document.NewReplacementUnit(Guid.NewGuid(), documentUnitId, periodType, usePreviousPeriod);

            if (opensNewValidityCycle)
                document.RegisterRenewal();

            return replacement;
        }

        public async Task CreateDocumentUnitsForEvent(Guid employeeId, Guid companyId, int eventId, CancellationToken cancellationToken = default)
        {
            var employee = await _employeeRepository.FirstOrDefaultMemoryOrDatabase(x => x.Id == employeeId && x.CompanyId == companyId) 
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), employeeId.ToString()));

            var requiredDocuments = await _requireDocumentsRepository.GetAllByCompanyEventAndAssociations(
                companyId, eventId, employee.GetAllPossibleAssociationIds(), cancellationToken);

            if (!requiredDocuments.Any())
            {
                _logger.LogInformation("No required documents found for employee {EmployeeId} and event {EventId}.", employeeId, eventId);
                return;
            }

            // A competência não vem do evento: quem decide é o template (via PeriodPolicy, lida na hora da
            // operação). O evento só dispara a geração, então tudo que ele fornece é o "agora" no fuso local —
            // a data que situa a unidade na competência corrente.
            var referenceDate = NowInLocalTime();

            // Coletar todos os templateIds necessários para uma única query
            var allTemplateIds = requiredDocuments
                .Where(rd => rd.StatusIsAccepted(eventId, employee.Status.Id))
                .SelectMany(rd => rd.DocumentsTemplatesIds)
                .Distinct()
                .ToList();

            if (!allTemplateIds.Any())
            {
                _logger.LogInformation("No accepted document templates for employee {EmployeeId} and event {EventId}.", employeeId, eventId);
                return;
            }

            // Carregar todos os documentos e templates de uma vez. TODOS os templates, mesmo os de documentos já
            // existentes: a configuração de competência é lida do template em toda criação de unidade, não só na
            // criação do documento.
            // Include das units: NewDocumentUnit deduplica varrendo DocumentsUnits — sem carregar a coleção, a
            // pendente equivalente nunca é encontrada e cada geração criaria uma unidade duplicada.
            var existingDocuments = await _documentRepository.GetDataAsync(
                x => allTemplateIds.Contains(x.DocumentTemplateId) && x.EmployeeId == employee.Id,
                include: i => i.Include(x => x.DocumentsUnits),
                cancellation: cancellationToken);

            var existingDocumentsByTemplateId = existingDocuments.ToDictionary(d => d.DocumentTemplateId);

            var documentTemplates = await _documentTemplateRepository.GetDataAsync(
                x => allTemplateIds.Contains(x.Id) && x.CompanyId == companyId,
                cancellation: cancellationToken);

            var documentTemplatesByTemplateId = documentTemplates.ToDictionary(dt => dt.Id);

            var documentsToInsert = new List<Document>();

            foreach (var requiredDocument in requiredDocuments)
            {
                if (!requiredDocument.StatusIsAccepted(eventId, employee.Status.Id))
                    continue;

                foreach (var templateId in requiredDocument.DocumentsTemplatesIds)
                {
                    if (!documentTemplatesByTemplateId.TryGetValue(templateId, out var documentTemplate))
                    {
                        _logger.LogWarning("Document template {TemplateId} not found for company {CompanyId}. Skipping.", templateId, companyId);
                        continue;
                    }

                    // Tentar obter documento existente ou criar novo
                    if (!existingDocumentsByTemplateId.TryGetValue(templateId, out var document))
                    {
                        var documentId = Guid.NewGuid();
                        document = Document.Create(
                            id: documentId,
                            employeeId: employee.Id,
                            companyId: companyId,
                            requiredDocumentId: requiredDocument.Id,
                            documentTemplateId: templateId,
                            name: documentTemplate.Name.Value,
                            description: documentTemplate.Description.Value);

                        documentsToInsert.Add(document);
                        existingDocumentsByTemplateId[templateId] = document; // Adicionar ao dicionário para evitar duplicatas
                    }

                    var (usePreviousPeriod, periodType) = PeriodConfigOf(documentTemplate);
                    var documentUnitId = Guid.NewGuid();
                    document.NewDocumentUnit(documentUnitId, periodType, usePreviousPeriod, referenceDate);
                }
            }

            // Inserir todos os novos documentos de uma vez
            if (documentsToInsert.Any())
            {
                await _documentRepository.InsertRangeAsync(documentsToInsert, cancellationToken);
       
                _logger.LogInformation("Created {Count} new documents for employee {EmployeeId} and event {EventId}.", 
                    documentsToInsert.Count, employeeId, eventId);
            }

        }

        public async Task GenerateDocumentUnitsForRequireDocument(Guid requireDocumentId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var requireDocument = await _requireDocumentsRepository.FirstOrDefaultAsync(
                x => x.Id == requireDocumentId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(RequireDocuments), requireDocumentId.ToString()));

            if (!requireDocument.DocumentsTemplatesIds.Any())
            {
                _logger.LogInformation("RequireDocument {RequireDocumentId} has no document templates. Skipping.", requireDocumentId);
                return;
            }

            Expression<Func<Employee, bool>> employeeFilter = requireDocument.AssociationType.Id == AssociationType.Role.Id
                ? e => e.CompanyId == companyId && requireDocument.AssociationIds.Contains(e.RoleId)
                : e => e.CompanyId == companyId && requireDocument.AssociationIds.Contains(e.WorkPlaceId);

            var employees = await _employeeRepository.GetDataAsync(employeeFilter, cancellation: cancellationToken);

            if (!employees.Any())
            {
                _logger.LogInformation("No employees found matching associations for RequireDocument {RequireDocumentId}.", requireDocumentId);
                return;
            }

            _logger.LogInformation("Generating document units for RequireDocument {RequireDocumentId} across {EmployeeCount} employees.",
                requireDocumentId, employees.Count());

            await Parallel.ForEachAsync(employees, cancellationToken, async (employee, ct) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedDocumentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                var scopedDocumentTemplateRepository = scope.ServiceProvider.GetRequiredService<IDocumentTemplateRepository>();

                // Include das units: NewDocumentUnit deduplica varrendo DocumentsUnits — sem carregar a coleção,
                // a pendente equivalente nunca é encontrada e cada geração criaria uma unidade duplicada.
                var existingDocuments = await scopedDocumentRepository.GetDataAsync(
                    x => requireDocument.DocumentsTemplatesIds.Contains(x.DocumentTemplateId) && x.EmployeeId == employee.Id,
                    include: i => i.Include(x => x.DocumentsUnits),
                    cancellation: ct);

                var existingDocumentsByTemplateId = existingDocuments.ToDictionary(d => d.DocumentTemplateId);

                // TODOS os templates, mesmo os de documentos já existentes: a configuração de competência é lida
                // do template em toda criação de unidade, não só na criação do documento.
                var documentTemplates = await scopedDocumentTemplateRepository.GetDataAsync(
                    x => requireDocument.DocumentsTemplatesIds.Contains(x.Id) && x.CompanyId == companyId,
                    cancellation: ct);

                var documentTemplatesByTemplateId = documentTemplates.ToDictionary(dt => dt.Id);

                var documentsToInsert = new List<Document>();

                foreach (var templateId in requireDocument.DocumentsTemplatesIds)
                {
                    if (!documentTemplatesByTemplateId.TryGetValue(templateId, out var documentTemplate))
                    {
                        _logger.LogWarning("Document template {TemplateId} not found for company {CompanyId}. Skipping.", templateId, companyId);
                        continue;
                    }

                    if (!existingDocumentsByTemplateId.TryGetValue(templateId, out var document))
                    {
                        var documentId = Guid.NewGuid();
                        document = Document.Create(
                            id: documentId,
                            employeeId: employee.Id,
                            companyId: companyId,
                            requiredDocumentId: requireDocument.Id,
                            documentTemplateId: templateId,
                            name: documentTemplate.Name.Value,
                            description: documentTemplate.Description.Value);

                        documentsToInsert.Add(document);
                        existingDocumentsByTemplateId[templateId] = document;
                    }

                    // Sem referenceDate: este fluxo não tem uma data de evento. Se o template for por
                    // competência, a unidade nasce na competência mínima, substituída quando uma data real chegar.
                    var (usePreviousPeriod, periodType) = PeriodConfigOf(documentTemplate);
                    var documentUnitId = Guid.NewGuid();
                    document.NewDocumentUnit(documentUnitId, periodType, usePreviousPeriod);
                }

                if (documentsToInsert.Any())
                {
                    await scopedDocumentRepository.InsertRangeAsync(documentsToInsert, ct);
                }

                await scopedDocumentRepository.UnitOfWork.SaveChangesAsync(ct);
            });

            _logger.LogInformation("Completed generating document units for RequireDocument {RequireDocumentId}.", requireDocumentId);
        }

        private DateTime NowInLocalTime()
            => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(_timeZone.TimeZoneId));

        // Lê a configuração de competência do template pela PeriodPolicy, no momento da operação. Null =
        // template não é por competência. Ninguém guarda cópia: template é a configuração, a unit é a história —
        // editar o template vale imediatamente para as próximas operações, e as competências já gravadas nas
        // units não mudam.
        private static (bool usePreviousPeriod, PeriodType? periodType) PeriodConfigOf(DocumentTemplate template)
        {
            var policy = template.GetPolicy<IPeriodPolicy>();
            return (policy?.UsePreviousPeriod ?? false, policy?.PeriodType);
        }

        // A validade da unidade vem da regra de vencimento do template, EXCETO quando os ciclos do documento já se
        // esgotaram: daí em diante toda unidade nasce SEM validade e fica OK indefinidamente, exatamente como num
        // template sem regra de vencimento.
        //
        // É aqui — e só aqui — que o teto do template age. Ele não recusa nada ao RH: renovar, substituir,
        // depreciar e invalidar continuam disponíveis; o que acaba é o vencimento, não a ação.
        //
        // Cruza dois aggregates (regra no template, contador no documento), então a decisão mora aqui e para o
        // aggregate desce só o valor.
        private static TimeSpan? ValidityDurationFor(DocumentTemplate template, Document document)
        {
            var expirationPolicy = template.GetPolicy<IExpirationPolicy>();

            if (expirationPolicy is null)
                return null;

            return expirationPolicy.HasValidityCycleLeft(document.RenewalCount)
                ? expirationPolicy.Duration
                : null;
        }

        public async Task<DocumentUnit> UpdateDocumentUnitDetails(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId,
            DateOnly documentUnitDate, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId
                && x.CompanyId == companyId, include: i => i.Include(x => x.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            if(document.IsPendingDocumentUnit(documentUnitId) == false)
                throw new DomainException(this, DomainErrors.Document.IsNotPending());

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == document.DocumentTemplateId 
            && x.CompanyId == companyId,
                cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            return await FillUnitDetails(document, documentTemplate, documentUnitId, documentUnitDate, employeeId,
                companyId, cancellationToken);
        }

        // Grava data, validade, competência e snapshot numa unidade pendente. Recebe o documento e o template já
        // carregados porque os dois callers (atualizar a data e criar a unidade de uma competência) já os têm em
        // mãos — recarregar aqui dispararia uma segunda query sobre a mesma unidade de trabalho.
        //
        // Escreve duas vezes de propósito: a primeira passada calcula a validade a partir da data, e é dela que o
        // construtor de conteúdo lê o vencimento que vai impresso no documento.
        private async Task<DocumentUnit> FillUnitDetails(Document document, DocumentTemplate documentTemplate,
            Guid documentUnitId, DateOnly documentUnitDate, Guid employeeId, Guid companyId,
            CancellationToken cancellationToken)
        {
            var workloadPolicy = documentTemplate.GetPolicy<IWorkloadPolicy>();
            var validityDuration = ValidityDurationFor(documentTemplate, document);
            var (usePreviousPeriod, periodType) = PeriodConfigOf(documentTemplate);

            DateOnly? workloadEndDate = null;
            if (workloadPolicy is not null && workloadPolicy.Workload != TimeSpan.Zero)
                workloadEndDate = await VerifyTimeConflictBetweenDocument(employeeId, companyId, document.Id, documentUnitDate,
                    workloadPolicy.Workload, cancellationToken);

            string? content = "";

            var documentUnit = document.UpdateDocumentUnitDetails(documentUnitId, documentUnitDate, validityDuration,
                content, periodType, usePreviousPeriod);

            if (workloadEndDate is not null)
                documentUnit.SetWorkloadEndDate(workloadEndDate.Value);

            if (documentTemplate.TemplateFileInfo is not null)
            {
                var contentResult = await _documentContentBuilder.Build(
                    documentTemplate.TemplateFileInfo.RecoversDataType,
                    employeeId,
                    companyId,
                    documentUnitDate,
                    documentUnit.Validity,
                    workloadEndDate,
                    cancellationToken);

                content = contentResult.Content;
            }

            documentUnit = document.UpdateDocumentUnitDetails(documentUnitId, documentUnitDate, validityDuration,
                content, periodType, usePreviousPeriod);

            if (workloadEndDate is not null)
                documentUnit.SetWorkloadEndDate(workloadEndDate.Value);

            return documentUnit;
        }

        public async Task<IReadOnlyList<DocumentUnitContentStatus>> CheckOutdatedContent(
            IEnumerable<(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)> items,
            Guid companyId, CancellationToken cancellationToken = default)
        {
            var itemsList = items.ToList();
            if (itemsList.Count == 0)
                return [];

            var documentIds = itemsList.Select(x => x.DocumentId).Distinct().ToList();

            var documents = (await _documentRepository.GetDataAsync(
                x => documentIds.Contains(x.Id) && x.CompanyId == companyId,
                include: i => i.Include(x => x.DocumentsUnits),
                cancellation: cancellationToken)).ToList();

            var documentById = documents.ToDictionary(d => d.Id);

            var templateIds = documents.Select(d => d.DocumentTemplateId).Distinct().ToList();
            var templateById = (await _documentTemplateRepository.GetDataAsync(
                x => templateIds.Contains(x.Id) && x.CompanyId == companyId,
                cancellation: cancellationToken)).ToDictionary(t => t.Id);

            var results = new List<DocumentUnitContentStatus>();

            foreach (var item in itemsList)
            {
                if (!documentById.TryGetValue(item.DocumentId, out var document) || document.EmployeeId != item.EmployeeId)
                    throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), item.DocumentId.ToString()));

                var documentUnit = document.DocumentsUnits.FirstOrDefault(x => x.Id == item.DocumentUnitId)
                    ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), item.DocumentUnitId.ToString()));

                if (!templateById.TryGetValue(document.DocumentTemplateId, out var documentTemplate))
                    throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

                // Template sem arquivo não gera documento, e unidade sem conteúdo ainda não teve snapshot: nos dois
                // casos não existe "antes" para comparar.
                if (documentTemplate.TemplateFileInfo is null || documentUnit.HasContent == false)
                {
                    results.Add(new DocumentUnitContentStatus(documentUnit.Id, false, false));
                    continue;
                }

                // As datas vêm da própria unidade, não do template: o que se quer detectar é dado do funcionário
                // que mudou, e recalcular a carga horária aqui exigiria a verificação de conflito, que escreve e
                // lança.
                var rebuilt = await _documentContentBuilder.Build(
                    documentTemplate.TemplateFileInfo.RecoversDataType,
                    document.EmployeeId,
                    companyId,
                    documentUnit.Date,
                    documentUnit.Validity,
                    documentUnit.WorkloadEndDate,
                    cancellationToken);

                if (rebuilt.IsComplete == false)
                {
                    results.Add(new DocumentUnitContentStatus(documentUnit.Id, false, true));
                    continue;
                }

                var isOutdated = string.Equals(rebuilt.Content, documentUnit.Content, StringComparison.Ordinal) == false;
                results.Add(new DocumentUnitContentStatus(documentUnit.Id, isOutdated, false));
            }

            return results;
        }

        public async Task RefreshDocumentUnitContent(
            IEnumerable<(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)> items,
            Guid companyId, CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == item.DocumentId
                    && x.EmployeeId == item.EmployeeId && x.CompanyId == companyId,
                    include: i => i.Include(x => x.DocumentsUnits), cancellation: cancellationToken)
                    ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), item.DocumentId.ToString()));

                var documentUnit = document.DocumentsUnits.FirstOrDefault(x => x.Id == item.DocumentUnitId)
                    ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), item.DocumentUnitId.ToString()));

                // Reaproveita a data já gravada: renovar o snapshot nunca move a data do documento.
                await UpdateDocumentUnitDetails(item.DocumentUnitId, item.DocumentId, item.EmployeeId, companyId,
                    documentUnit.Date, cancellationToken);
            }
        }

        public async Task<byte[]> GeneratePdf(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId,
            CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId 
                && x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            if (document.IsPendingDocumentUnit(documentUnitId) == false)
                throw new DomainException(this, DomainErrors.Document.IsNotPending());


            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == document.DocumentTemplateId 
                && x.CompanyId == companyId, cancellation: cancellationToken)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            if (documentTemplate.TemplateFileInfo is null)
                throw new DomainException(this, DomainErrors.Document.DocumentNotHaveTemplate(documentId));

            var documentUnit = document.DocumentsUnits.First(x => x.Id == documentUnitId);

            if (documentUnit.HasContent == false)
                throw new DomainException(this, DomainErrors.Document.ErrorRecoverData(documentUnitId));
            
            var pdfBytes = await _pdfService.ConvertHtml2Pdf(documentTemplate.TemplateFileInfo, documentUnit.Content, cancellationToken);

            return pdfBytes;
        }

        public async Task<IReadOnlyList<(Guid DocumentUnitId, Guid DocumentId, string DocumentName, DateOnly DocumentUnitDate, byte[] Pdf)>> GeneratePdfRange(
            IEnumerable<(Guid DocumentId, IEnumerable<Guid> DocumentUnitIds)> items,
            Guid employeeId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var itemsList = items.ToList();
            var documentIds = itemsList.Select(x => x.DocumentId).ToList();
            var unitIdsByDocument = itemsList.ToDictionary(x => x.DocumentId, x => x.DocumentUnitIds.ToHashSet());

            var documents = (await _documentRepository.GetDataAsync(
                x => documentIds.Contains(x.Id) && x.EmployeeId == employeeId && x.CompanyId == companyId,
                include: x => x.Include(y => y.DocumentsUnits),
                cancellation: cancellationToken)).ToList();

            var templateIds = documents.Select(d => d.DocumentTemplateId).Distinct().ToList();
            var templateById = (await _documentTemplateRepository.GetDataAsync(
                x => templateIds.Contains(x.Id) && x.CompanyId == companyId,
                cancellation: cancellationToken)).ToDictionary(t => t.Id);

            var pdfItems = new List<(Guid DocumentUnitId, Guid DocumentId, string DocumentName, DateOnly DocumentUnitDate, TemplateFileInfo Template, string Content)>();

            foreach (var document in documents)
            {
                if (!unitIdsByDocument.TryGetValue(document.Id, out var unitIds)) continue;

                if (!templateById.TryGetValue(document.DocumentTemplateId, out var template))
                    throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

                if (template.TemplateFileInfo is null)
                    throw new DomainException(this, DomainErrors.Document.DocumentNotHaveTemplate(document.Id));

                foreach (var unit in document.DocumentsUnits.Where(u => unitIds.Contains(u.Id)))
                {
                    if (!document.IsPendingDocumentUnit(unit.Id))
                        throw new DomainException(this, DomainErrors.Document.IsNotPending());

                    if (!unit.HasContent)
                        throw new DomainException(this, DomainErrors.Document.ErrorRecoverData(unit.Id));

                    pdfItems.Add((unit.Id, document.Id, document.Name.ToString(), unit.Date, template.TemplateFileInfo, unit.Content));
                }
            }

            var pdfResults = await _pdfService.ConvertHtml2PdfRange(
                pdfItems.Select(x => (x.DocumentUnitId, x.Template, x.Content)),
                cancellationToken);

            var metadataByUnit = pdfItems.ToDictionary(
                x => x.DocumentUnitId,
                x => (x.DocumentId, x.DocumentName, x.DocumentUnitDate));

            return pdfResults
                .Select(r =>
                {
                    var meta = metadataByUnit[r.DocumentUnitId];
                    return (r.DocumentUnitId, meta.DocumentId, meta.DocumentName, meta.DocumentUnitDate, r.Pdf);
                })
                .ToList();
        }

        public async Task InsertFileWithoutRequireValidation(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId,
            Extension extension, Stream stream, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId && 
                x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            if (document.IsPendingDocumentUnit(documentUnitId) == false)
                throw new DomainException(this, DomainErrors.Document.IsNotPending());

            var fileName = Guid.NewGuid().ToString();

            string fileNameWithExtesion = document.InsertUnitWithoutRequireValidation(documentUnitId, fileName, extension);

            await _blobService.UploadAsync(stream, fileNameWithExtesion, document.CompanyId.ToString(), overwrite: false, cancellationToken: cancellationToken);
        }


        private async Task<DateOnly> VerifyTimeConflictBetweenDocument(Guid employeeId, Guid companyId, Guid documentId, DateOnly documentUnitDate,
            TimeSpan workload, CancellationToken cancellationToken)
        {
            var maxHours = _documentTemplatesOptions.MaxHoursWorkload;

            if (!_workloadCalendarService.IsWorkingDay(documentUnitDate))
                throw new DomainException(this, DomainErrors.Document.WorkloadDateNotWorkingDay(documentUnitDate,
                    _workloadCalendarService.GetNextWorkingDay(documentUnitDate)));

            var projectedPeriod = _workloadCalendarService.DistributeWorkload(documentUnitDate, workload, maxHours);

            var documents = await _documentRepository.GetDataAsync(x => x.Id != documentId && x.EmployeeId == employeeId && x.CompanyId == companyId &&
                x.DocumentsUnits.Any(d => d.Date <= projectedPeriod.EndDate && (d.WorkloadEndDate ?? d.Date) >= documentUnitDate),
                include: i => i.Include(x => x.DocumentsUnits), cancellation: cancellationToken);

            var existingUsage = new Dictionary<DateOnly, TimeSpan>();

            foreach (var document in documents)
            {
                DocumentTemplate? documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == document.DocumentTemplateId
                    && x.CompanyId == companyId,
                    cancellation: cancellationToken);

                var templateWorkloadPolicy = documentTemplate?.GetPolicy<IWorkloadPolicy>();

                if (templateWorkloadPolicy == null)
                    continue;

                foreach (var unit in document.DocumentsUnits)
                {

                    if(unit.Status == DocumentUnitStatus.Invalid || unit.Status == DocumentUnitStatus.NotApplicable
                        || unit.Status == DocumentUnitStatus.Deprecated)
                    {
                        continue;
                    }

                    var unitPeriod = _workloadCalendarService.DistributeWorkload(unit.Date, templateWorkloadPolicy.Workload, maxHours);
                    var currentDate = unit.Date;
                    var remaining = templateWorkloadPolicy.Workload.TotalHours;

                    while (remaining > 0)
                    {
                        if (!_workloadCalendarService.IsWorkingDay(currentDate))
                        {
                            currentDate = _workloadCalendarService.GetNextWorkingDay(currentDate);
                            continue;
                        }

                        var hoursToAllocate = Math.Min(remaining, maxHours);
                        if (existingUsage.TryGetValue(currentDate, out var current))
                            existingUsage[currentDate] = current.Add(TimeSpan.FromHours(hoursToAllocate));
                        else
                            existingUsage[currentDate] = TimeSpan.FromHours(hoursToAllocate);

                        remaining -= hoursToAllocate;
                        currentDate = currentDate.AddDays(1);
                    }
                }
            }

            var fitResult = _workloadCalendarService.TryFitWorkload(documentUnitDate, workload, maxHours, existingUsage);

            if (!fitResult.CanFit)
            {
                var suggestedDate = fitResult.SuggestedStartDate ?? _workloadCalendarService.GetNextWorkingDay(documentUnitDate);
                throw new DomainException(this, DomainErrors.Document.TimeConflictBetweenDocuments(documentId,
                    TimeSpan.FromHours(maxHours), suggestedDate));
            }

            return fitResult.WorkloadEndDate;
        }





    }
}
