using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
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
        IEmployeeRepository employeeRepository, ILogger<DocumentService> logger) : IDocumentService
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
                ?? throw new ArgumentNullException(nameof(Employee));

            var requiedDocuments = await _requireDocumentsRepository.GetAllWithEventId(employeeId, companyId, eventId, cancellationToken);

            foreach(var requiedDocument in requiedDocuments)
            {
               
                var isAccepted = requiedDocument.StatusIsAccepted(eventId, employee.Status.Id);

                if (isAccepted == false)
                    continue;

                Document? document = await _documentRepository.FirstOrDefaultMemoryOrDatabase(x => x.EmployeeId == employeeId 
                && x.CompanyId == companyId && x.RequiredDocumentId == requiedDocument.Id, include: i => i.Include(x => x.DocumentsUnits));
               
                if(document is null)
                {
                    _logger.LogError("Document not found for employee {EmployeeId} and required document {RequiredDocumentId}. Skipping document creation.",
                        employeeId, requiedDocument.Id);
                    continue;
                }

                var documentUnitId = Guid.NewGuid();
                
                document!.NewDocumentUnit(documentUnitId);
            }

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

            string? content = "";
            var documentUnit = document.UpdateDocumentUnitDetails(documentUnitId, documentUnitDate, documentTemplate.DocumentValidityDuration,
                content);

            
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

            await _blobService.UploadAsync(stream, fileNameWithExtesion, document.CompanyId.ToString(), cancellationToken);
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
                    return null;                    
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


    }
}
