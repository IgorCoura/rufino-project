using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Services.FactoryImpl.RecoverInfoToSegurityDocument;

namespace PeopleManagement.Services.Services
{
    public class SecurityDocumentService : ISecurityDocumentService
    {
        private readonly ISecurityDocumentRepository _securityDocumentRepository;
        private readonly IHtmlService _htmlService;
        private readonly IServiceProvider _serviceProvider;

        public SecurityDocumentService(ISecurityDocumentRepository securityDocumentRepository, IServiceProvider serviceProvider, IHtmlService htmlService)
        {
            _securityDocumentRepository = securityDocumentRepository;
            _serviceProvider = serviceProvider;
            _htmlService = htmlService;
        }

        public async void CreateDocument(Guid securityDocumentId, Guid employeeId, Guid companyId, DateTime documentDate)
        {
            var securityDocument = await _securityDocumentRepository.FirstOrDefaultAsync(x => x.Id == securityDocumentId && x.EmployeeId == employeeId && x.CompanyId == companyId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(SecurityDocument), securityDocumentId.ToString()));

            var documentId = Guid.NewGuid();
            var service = GetService(securityDocument.Type, _serviceProvider);
            var values = await service.RecoverInfo();
            var htmlContent = await _htmlService.CreateHtml(securityDocument.Type, values);
            securityDocument.CreateDocument(documentId, htmlContent, documentDate);
        }

        private static IRecoverInfoToSegurityDocumentService GetService(DocumentType doc, IServiceProvider provider)
        {
            var result = provider.GetService(doc.Type) as IRecoverInfoToSegurityDocumentService ?? throw new NullReferenceException($"O Serviço de tipo {doc.Type} não foi invejtado.");
            return result;
        }
    }
}
