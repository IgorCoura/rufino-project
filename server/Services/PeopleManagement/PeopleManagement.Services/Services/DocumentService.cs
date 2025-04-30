using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using Extension = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Extension;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Services.Services
{
    public class DocumentService(IDocumentRepository securityDocumentRepository, IServiceProvider serviceProvider, 
        IPdfService pdfService, IBlobService blobService, IDocumentTemplateRepository documentTemplateRepository,
        DocumentTemplatesOptions documentTemplatesOptions, IRequireDocumentsRepository requireDocumentsRepository,
        IEmployeeRepository employeeRepository) : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository = securityDocumentRepository;
        private readonly IPdfService _pdfService = pdfService;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IBlobService _blobService = blobService;
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;
        private readonly DocumentTemplatesOptions _documentTemplatesOptions = documentTemplatesOptions;
        private readonly IRequireDocumentsRepository _requireDocumentsRepository = requireDocumentsRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

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
            var employee = await _employeeRepository.FirstOrDefaultMemoryOrDatabase(x => x.Id == employeeId && x.CompanyId == companyId) ?? throw new ArgumentNullException(nameof(Employee));
            var requiedDocuments = await _requireDocumentsRepository.GetAllWithEventId(employeeId, companyId, eventId, cancellationToken);

            foreach(var requiedDocument in requiedDocuments)
            {
                var listenEvent = requiedDocument.ListenEvents.Find(x => x.EventId == eventId);

                if (listenEvent!.Status.Contains(employee.Status.Id) == false)
                    continue;

                Document? document = await _documentRepository.FirstOrDefaultMemoryOrDatabase(x => x.EmployeeId == employeeId 
                && x.CompanyId == companyId && x.RequiredDocumentId == requiedDocument.Id);
               
                if(document is null)
                    continue;

                var documentUnitId = Guid.NewGuid();
                
                document!.NewDocumentUnit(documentUnitId);
            }

        }


        public async Task<DocumentUnit> UpdateDocumentUnitDetails(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, 
            DateTime documentUnitDate, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId
                && x.CompanyId == companyId, include: i => i.Include(x => x.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == document.DocumentTemplateId 
            && x.CompanyId == companyId,
                cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            if (documentTemplate.Workload is not null)
                await VerifyTimeConflictBetweenDocument(employeeId, companyId, documentId, documentUnitDate, 
                    (TimeSpan)documentTemplate.Workload, cancellationToken);

            var recoverDataService = GetServiceToRecoverData(documentTemplate.TemplateFileInfo.RecoverDataType, _serviceProvider);
            var content = await recoverDataService.RecoverInfo(document.EmployeeId, document.CompanyId, documentUnitDate, cancellationToken);
            var documentUnit = document.UpdateDocumentUnitDetails(documentUnitId, documentUnitDate, documentTemplate.DocumentValidityDuration, 
                content);
            return documentUnit;
        }

        public async Task<byte[]> GeneratePdf(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, 
            CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId 
                && x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == document.DocumentTemplateId 
                && x.CompanyId == companyId, cancellation: cancellationToken)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            var documentUnit = document.DocumentsUnits.First(x => x.Id == documentUnitId);
            
            var pdfBytes = await _pdfService.ConvertHtml2Pdf(documentTemplate, documentUnit.Content, cancellationToken);
          
            return pdfBytes;
        }

        public async Task InsertFileWithoutRequireValidation(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, Extension extension, Stream stream, 
            CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId && 
                x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));


            var fileName = Guid.NewGuid().ToString();

            string fileNameWithExtesion = document.InsertUnitWithoutRequireValidation(documentUnitId, fileName, extension);

            await _blobService.UploadAsync(stream, fileNameWithExtesion, document.CompanyId.ToString(), cancellationToken);
        }

        
        private static IRecoverInfoToDocumentTemplateService GetServiceToRecoverData(RecoverDataType doc, IServiceProvider provider)
        {
            var result = provider.GetRequiredService(doc.Type) as IRecoverInfoToDocumentTemplateService 
                ?? throw new NullReferenceException($"O Serviço de tipo {doc.Type} não foi injetado.");
            return result;
        }

        private async Task VerifyTimeConflictBetweenDocument(Guid employeeId, Guid companyId, Guid documentId, DateTime documentUnitDate, TimeSpan workload, 
            CancellationToken cancellationToken)
        {
            var documents = await _documentRepository.GetDataAsync(x => x.Id != documentId && x.EmployeeId == employeeId && x.CompanyId == companyId &&
                x.DocumentsUnits.Any(d => DateOnly.FromDateTime(d.Date) == DateOnly.FromDateTime(documentUnitDate)), include: i => i.Include(x => x.DocumentsUnits),
                cancellation: cancellationToken);

            foreach(var document in documents)
            {
                DocumentTemplate? documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == document.DocumentTemplateId && x.CompanyId == companyId, 
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
