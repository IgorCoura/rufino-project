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
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.Options;
using PeopleManagement.Domain.Utils;
using System.Collections.Generic;
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
        IEmployeeRepository employeeRepository, IOptions<TimeZoneOptions> timeZoneOptions, ILogger<DocumentService> logger) : IDocumentService
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

        public async Task<DocumentUnit> CreateDocumentUnit(Guid documentId, Guid employeeId, Guid companyId, 
            CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId 
                && x.CompanyId == companyId, include: i => i.Include(x => x.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            var documentUnitId = Guid.NewGuid();

            return document.NewDocumentUnit(documentUnitId);
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

            var (periodType, referenceDate) = GetPeriodInfoFromEvent(eventId);

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

            // Carregar todos os documentos e templates de uma vez
            var existingDocuments = await _documentRepository.GetDataAsync(
                x => allTemplateIds.Contains(x.DocumentTemplateId) && x.EmployeeId == employee.Id,
                cancellation: cancellationToken);

            var existingDocumentsByTemplateId = existingDocuments.ToDictionary(d => d.DocumentTemplateId);

            // Obter apenas os templateIds que não têm documentos criados
            var templateIdsWithoutDocuments = allTemplateIds
                .Except(existingDocumentsByTemplateId.Keys)
                .ToList();

            // Carregar apenas os templates necessários (que não têm documentos)
            var documentTemplates = templateIdsWithoutDocuments.Any()
                ? await _documentTemplateRepository.GetDataAsync(
                    x => templateIdsWithoutDocuments.Contains(x.Id) && x.CompanyId == companyId,
                    cancellation: cancellationToken)
                : [];

            var documentTemplatesByTemplateId = documentTemplates.ToDictionary(dt => dt.Id);

            var documentsToInsert = new List<Document>();

            foreach (var requiredDocument in requiredDocuments)
            {
                if (!requiredDocument.StatusIsAccepted(eventId, employee.Status.Id))
                    continue;

                foreach (var templateId in requiredDocument.DocumentsTemplatesIds)
                {
                    // Tentar obter documento existente ou criar novo
                    if (!existingDocumentsByTemplateId.TryGetValue(templateId, out var document))
                    {
                        if (!documentTemplatesByTemplateId.TryGetValue(templateId, out var documentTemplate))
                        {
                            _logger.LogWarning("Document template {TemplateId} not found for company {CompanyId}. Skipping.", templateId, companyId);
                            continue;
                        }

                        var documentId = Guid.NewGuid();
                        document = Document.Create(
                            documentId, 
                            employee.Id, 
                            companyId, 
                            requiredDocument.Id, 
                            templateId,
                            documentTemplate.Name.Value, 
                            documentTemplate.Description.Value,
                            documentTemplate.UsePreviousPeriod);

                        documentsToInsert.Add(document);
                        existingDocumentsByTemplateId[templateId] = document; // Adicionar ao dicionário para evitar duplicatas
                    }

                    var documentUnitId = Guid.NewGuid();
                    document.NewDocumentUnit(documentUnitId, periodType, referenceDate);
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

        private (PeriodType? periodType, DateTime? referenceDate) GetPeriodInfoFromEvent(int eventId)
        {
            var recurringEvent = RecurringEvents.FromValue(eventId);
            var eventFrequency = recurringEvent?.GetFrequency() ?? RecurringEventFrequency.None;
            var periodType = ConvertFrequencyToPeriodType(eventFrequency);
            
            DateTime? referenceDate = periodType != null 
                ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(_timeZone.TimeZoneId)) 
                : null;

            return (periodType, referenceDate);
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

            if (documentTemplate.Workload is not null)
                await VerifyTimeConflictBetweenDocument(employeeId, companyId, documentId, documentUnitDate, 
                    (TimeSpan)documentTemplate.Workload, cancellationToken);

            var requiredDocument = await _requireDocumentsRepository.FirstOrDefaultAsync(x => x.Id == document.RequiredDocumentId 
                && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(RequireDocuments), document.RequiredDocumentId.ToString()));

            var recurringEventFrequency = RecurringEvents.GetUniqueRecurringEventsFrequency(requiredDocument.ListenEvents.Select(x => x.EventId));
            var periodType = ConvertFrequencyToPeriodType(recurringEventFrequency);
            string? content = "";

            DocumentUnit documentUnit;

            if(periodType != null)
            {
                documentUnit = document.UpdateDocumentUnitDetails(documentUnitId, documentUnitDate, documentTemplate.DocumentValidityDuration,
                content, periodType);

            }
            else
            {
                documentUnit = document.UpdateDocumentUnitDetails(documentUnitId, documentUnitDate, documentTemplate.DocumentValidityDuration,
                content);
            }


            if(documentTemplate.TemplateFileInfo is not null)
            {
                content = await RecoverInfoToDocument(
                    documentTemplate.TemplateFileInfo.RecoversDataType,
                    employeeId,
                    companyId,
                    jsonObjects: [
                        new JsonObject{
                            ["date"] = $"{documentUnitDate}",
                            ["validity"] = $"{documentUnit.Validity}"
                        },
                        ],
                    cancellationToken: cancellationToken);

                if (content == null)
                {
                    throw new DomainException(this, DomainErrors.Document.ErrorRecoverData(documentId));
                }
                
            }

            documentUnit = document.UpdateDocumentUnitDetails(documentUnitId, documentUnitDate, documentTemplate.DocumentValidityDuration,
                content);

            return documentUnit;
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


        private async Task<string?> RecoverInfoToDocument(List<RecoverDataType> recoverDataTypes, Guid employeeId, Guid companyId, 
            JsonObject[]? jsonObjects = null, CancellationToken cancellationToken = default)
        {
            var objects  = new List<JsonObject>();
            if(jsonObjects != null)
            {
                var recoverDataService = GetServiceToRecoverData(RecoverDataType.ComplementaryInfo, _serviceProvider);
                var jsonObject = await recoverDataService.RecoverInfo(employeeId, companyId, jsonObjects: jsonObjects, cancellation: cancellationToken);
                objects.Add(jsonObject);
            }
                
            foreach (var recoverDataType in recoverDataTypes)
            {
                try
                {
                    if(recoverDataType == RecoverDataType.ComplementaryInfo)
                    {
                        continue;
                    }
                    var recoverDataService = GetServiceToRecoverData(recoverDataType, _serviceProvider);
                    var jsonObject = await recoverDataService.RecoverInfo(employeeId, companyId, cancellation: cancellationToken);
                    objects.Add(jsonObject);
                }
                catch(Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to recover data of type {RecoverDataType} for employee {EmployeeId} in company {CompanyId}. Skipping this data type.",
                        recoverDataType.Name, employeeId, companyId);
                    continue;                    
                }
            }
            var result = objects.MergeListJsonObjects();
            return result.ToString();
        }


        private static IRecoverInfoToDocumentTemplateService GetServiceToRecoverData(RecoverDataType doc, IServiceProvider provider)
        {
            var result = provider.GetRequiredService(doc.Type) as IRecoverInfoToDocumentTemplateService 
                ?? throw new NullReferenceException($"O Serviço de tipo {doc.Type} não foi injetado.");
            return result;
        }

        private async Task VerifyTimeConflictBetweenDocument(Guid employeeId, Guid companyId, Guid documentId, DateOnly documentUnitDate, 
            TimeSpan workload, CancellationToken cancellationToken)
        {
            var documents = await _documentRepository.GetDataAsync(x => x.Id != documentId && x.EmployeeId == employeeId && x.CompanyId == companyId &&
                x.DocumentsUnits.Any(d => d.Date == documentUnitDate), 
                include: i => i.Include(x => x.DocumentsUnits),cancellation: cancellationToken);

            foreach(var document in documents)
            {
                DocumentTemplate? documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == document.DocumentTemplateId 
                && x.CompanyId == companyId, 
                    cancellation: cancellationToken);

                if (documentTemplate is null || documentTemplate.Workload == null)
                    continue;

                if (workload.Add((TimeSpan)documentTemplate.Workload) > TimeSpan.FromHours(_documentTemplatesOptions.MaxHoursWorkload))
                    throw new DomainException(this, DomainErrors.Document.TimeConflictBetweenDocuments(documentId, document.Id, 
                        TimeSpan.FromHours(_documentTemplatesOptions.MaxHoursWorkload)));
            }
        }

        private static PeriodType? ConvertFrequencyToPeriodType(RecurringEventFrequency frequency)
        {
            return frequency switch
            {
                RecurringEventFrequency.Daily => PeriodType.Daily,
                RecurringEventFrequency.Weekly => PeriodType.Weekly,
                RecurringEventFrequency.Monthly => PeriodType.Monthly,
                RecurringEventFrequency.Yearly => PeriodType.Yearly,
                RecurringEventFrequency.None => null,
                _ => null
            };
        }




    }
}
