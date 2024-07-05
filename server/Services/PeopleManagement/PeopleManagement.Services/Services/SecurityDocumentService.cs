using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Services.Services
{
    public class SecurityDocumentService : ISecurityDocumentService
    {
        private readonly ISecurityDocumentRepository _securityDocumentRepository;
        private readonly IPdfService _pdfService;
        private readonly IServiceProvider _serviceProvider;

        public SecurityDocumentService(ISecurityDocumentRepository securityDocumentRepository, IServiceProvider serviceProvider, IPdfService pdfService)
        {
            _securityDocumentRepository = securityDocumentRepository;
            _serviceProvider = serviceProvider;
            _pdfService = pdfService;
        }

        public async Task<Document> CreateDocument(Guid securityDocumentId, Guid employeeId, Guid companyId, DateTime documentDate, CancellationToken cancellationToken)
        {
            var securityDocument = await _securityDocumentRepository.FirstOrDefaultAsync(x => x.Id == securityDocumentId && x.EmployeeId == employeeId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(SecurityDocument), securityDocumentId.ToString()));

            var documentId = Guid.NewGuid();
            var service = GetService(securityDocument.Type, _serviceProvider);            
            var content = await service.RecoverInfo(securityDocument.EmployeeId, securityDocument.CompanyId, documentDate, cancellationToken);
            var document = Document.Create(documentId, content, documentDate, securityDocument);
            securityDocument.AddDocument(document);

            return document;
        }

        public async Task<byte[]> GeneratePdf(Guid documentId, Guid securityDocumentId, Guid employeeId, Guid companyId, CancellationToken cancellationToken)
        {
            var securityDocument = await _securityDocumentRepository.FirstOrDefaultAsync(x => x.Id == securityDocumentId && x.EmployeeId == employeeId && x.CompanyId == companyId, include: x => x.Include(y => y.Documents), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(SecurityDocument), securityDocumentId.ToString()));

            var document = securityDocument.Documents.First(x => x.Id == documentId);
            var pdfBytes = await _pdfService.ConvertHtml2Pdf(securityDocument.Type, document.Content, cancellationToken);
            return pdfBytes;
        }

        private static IRecoverInfoToSecurityDocumentService GetService(DocumentType doc, IServiceProvider provider)
        {
            var result = provider.GetRequiredService(doc.Type) as IRecoverInfoToSecurityDocumentService ?? throw new NullReferenceException($"O Serviço de tipo {doc.Type} não foi invejtado.");
            return result;
        }
    }
}
