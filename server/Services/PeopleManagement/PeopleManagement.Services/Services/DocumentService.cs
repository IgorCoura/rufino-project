using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using Extension = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Extension;

namespace PeopleManagement.Services.Services
{
    public class DocumentService(IDocumentRepository securityDocumentRepository, IServiceProvider serviceProvider, 
        IPdfService pdfService, IBlobService blobService, IDocumentTemplateRepository documentTemplateRepository) : IDocumentService
    {
        private readonly IDocumentRepository _securityDocumentRepository = securityDocumentRepository;
        private readonly IPdfService _pdfService = pdfService;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IBlobService _blobService = blobService;
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;

        public async Task<DocumentUnit> CreateDocument(Guid securityDocumentId, Guid employeeId, Guid companyId, DateTime documentDate, CancellationToken cancellationToken = default)
        {
            var securityDocument = await _securityDocumentRepository.FirstOrDefaultAsync(x => x.Id == securityDocumentId && x.EmployeeId == employeeId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), securityDocumentId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == securityDocument.DocumentTemplateId && x.CompanyId == companyId, cancellation: cancellationToken) 
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), securityDocument.DocumentTemplateId.ToString()));

            var documentId = Guid.NewGuid();
            var recoverDataService = GetServiceToRecoverData(documentTemplate.RecoverDataType, _serviceProvider);
            var content = await recoverDataService.RecoverInfo(securityDocument.EmployeeId, securityDocument.CompanyId, documentDate, cancellationToken);
            var document = DocumentUnit.Create(documentId, content, documentDate, securityDocument);
            securityDocument.AddDocument(document);

            return document;
        }

        public async Task<byte[]> GeneratePdf(Guid documentId, Guid securityDocumentId, Guid employeeId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var securityDocument = await _securityDocumentRepository.FirstOrDefaultAsync(x => x.Id == securityDocumentId && x.EmployeeId == employeeId && x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), securityDocumentId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == securityDocument.DocumentTemplateId && x.CompanyId == companyId, cancellation: cancellationToken)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), securityDocument.DocumentTemplateId.ToString()));

            var document = securityDocument.DocumentsUnits.First(x => x.Id == documentId);
            var pdfBytes = await _pdfService.ConvertHtml2Pdf(documentTemplate, document.Content, cancellationToken);
            return pdfBytes;
        }

        public async Task InsertFileWithoutRequireValidation(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, Extension extension, Stream stream, CancellationToken cancellationToken = default)
        {
            var securityDocument = await _securityDocumentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId && x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == securityDocument.DocumentTemplateId && x.CompanyId == companyId, cancellation: cancellationToken)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), securityDocument.DocumentTemplateId.ToString()));

            var fileName = Guid.NewGuid().ToString();

            string fileNameWithExtesion = securityDocument.InsertUnitWithoutRequireValidation(documentUnitId, fileName, extension, documentTemplate.DocumentValidityDuration);

            await _blobService.UploadAsync(stream, fileNameWithExtesion, securityDocument.CompanyId.ToString(), cancellationToken);
        }

        private static IRecoverInfoToDocumentTemplateService GetServiceToRecoverData(RecoverDataType doc, IServiceProvider provider)
        {
            var result = provider.GetRequiredService(doc.Type) as IRecoverInfoToDocumentTemplateService ?? throw new NullReferenceException($"O Serviço de tipo {doc.Type} não foi invejtado.");
            return result;
        }
    }
}
