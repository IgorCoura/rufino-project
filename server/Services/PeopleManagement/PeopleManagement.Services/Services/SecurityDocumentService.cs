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

        public async Task<byte[]> CreateDocument(Guid securityDocumentId, Guid employeeId, Guid companyId, DateTime documentDate)
        {
            var securityDocument = await _securityDocumentRepository.FirstOrDefaultAsync(x => x.Id == securityDocumentId && x.EmployeeId == employeeId && x.CompanyId == companyId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(SecurityDocument), securityDocumentId.ToString()));

            var documentId = Guid.NewGuid();
            var service = GetService(securityDocument.Type, _serviceProvider);
            var content = await service.RecoverInfo();
            var document = Document.Create(documentId, content, documentDate);
            securityDocument.AddDocument(document);
            var pdfBytes = await _pdfService.ConvertHtml2Pdf(securityDocument.Type, content);
            return pdfBytes;
        }

        private static IRecoverInfoToSegurityDocumentService GetService(DocumentType doc, IServiceProvider provider)
        {
            var result = provider.GetService(doc.Type) as IRecoverInfoToSegurityDocumentService ?? throw new NullReferenceException($"O Serviço de tipo {doc.Type} não foi invejtado.");
            return result;
        }
    }
}
