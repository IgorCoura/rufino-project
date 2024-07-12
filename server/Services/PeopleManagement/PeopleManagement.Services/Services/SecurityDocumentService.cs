using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Options;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using Extension = PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Extension;

namespace PeopleManagement.Services.Services
{
    public class SecurityDocumentService(ISecurityDocumentRepository securityDocumentRepository, IServiceProvider serviceProvider, IPdfService pdfService, IBlobService blobService, SecurityDocumentsFilesOptions securityDocumentsFilesOptions, IDocumentTemplateRepository documentTemplateRepository) : ISecurityDocumentService
    {
        private readonly ISecurityDocumentRepository _securityDocumentRepository = securityDocumentRepository;
        private readonly IPdfService _pdfService = pdfService;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IBlobService _blobService = blobService;
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;
        private readonly SecurityDocumentsFilesOptions _securityDocumentsFilesOptions = securityDocumentsFilesOptions;

        public async Task<Document> CreateDocument(Guid securityDocumentId, Guid employeeId, Guid companyId, DateTime documentDate, CancellationToken cancellationToken = default)
        {
            var securityDocument = await _securityDocumentRepository.FirstOrDefaultAsync(x => x.Id == securityDocumentId && x.EmployeeId == employeeId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(SecurityDocument), securityDocumentId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == securityDocument.DocumentTemplateId && x.CompanyId == companyId, cancellation: cancellationToken) 
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), securityDocument.DocumentTemplateId.ToString()));

            var documentId = Guid.NewGuid();
            var recoverDataService = GetServiceToRecoverData(documentTemplate.RecoverDataType, _serviceProvider);
            var content = await recoverDataService.RecoverInfo(securityDocument.EmployeeId, securityDocument.CompanyId, documentDate, cancellationToken);
            var document = Document.Create(documentId, content, documentDate, securityDocument);
            securityDocument.AddDocument(document);

            return document;
        }

        public async Task<byte[]> GeneratePdf(Guid documentId, Guid securityDocumentId, Guid employeeId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var securityDocument = await _securityDocumentRepository.FirstOrDefaultAsync(x => x.Id == securityDocumentId && x.EmployeeId == employeeId && x.CompanyId == companyId, include: x => x.Include(y => y.Documents), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(SecurityDocument), securityDocumentId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == securityDocument.DocumentTemplateId && x.CompanyId == companyId, cancellation: cancellationToken)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), securityDocument.DocumentTemplateId.ToString()));

            var document = securityDocument.Documents.First(x => x.Id == documentId);
            var pdfBytes = await _pdfService.ConvertHtml2Pdf(documentTemplate, document.Content, cancellationToken);
            return pdfBytes;
        }

        public async Task InsertFileWithoutRequireValidation(Guid documentId, Guid securityDocumentId, Guid employeeId, Guid companyId, string stringExtension, Stream stream, CancellationToken cancellationToken = default)
        {
            var securityDocument = await _securityDocumentRepository.FirstOrDefaultAsync(x => x.Id == securityDocumentId && x.EmployeeId == employeeId && x.CompanyId == companyId, include: x => x.Include(y => y.Documents), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(SecurityDocument), securityDocumentId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == securityDocument.DocumentTemplateId && x.CompanyId == companyId, cancellation: cancellationToken)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), securityDocument.DocumentTemplateId.ToString()));


            var extension = Extension.Create(stringExtension.Replace(".", ""));
            var fileName = Guid.NewGuid().ToString();

            string fileNameWithExtesion = securityDocument.InsertDocumentWithoutRequireValidation(documentId, fileName, extension, documentTemplate.DocumentValidityDuration);

            await _blobService.UploadAsync(stream, fileNameWithExtesion, _securityDocumentsFilesOptions.DocumentsContainer, cancellationToken);
        }

        private static IRecoverInfoToSecurityDocumentService GetServiceToRecoverData(RecoverDataType doc, IServiceProvider provider)
        {
            var result = provider.GetRequiredService(doc.Type) as IRecoverInfoToSecurityDocumentService ?? throw new NullReferenceException($"O Serviço de tipo {doc.Type} não foi invejtado.");
            return result;
        }
    }
}
