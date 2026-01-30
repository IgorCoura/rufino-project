using Hangfire;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Text.Json.Nodes;
using Document = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Document;
using Employee = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Employee;

namespace PeopleManagement.Services.Services
{
    public class SignDocumentService(IDocumentSignatureService documentSignatureService, IDocumentRepository documentRepository, 
        ICompanyRepository companyRepository, IEmployeeRepository employeeRepository, IDocumentTemplateRepository documentTemplateRepository, 
        IBlobService blobService,IDocumentService documentService , IWebHookManagementService webHookManagementService, IFileDownloadService fileDownloadService,
        IBackgroundJobClient backgroundJobClient) : ISignDocumentService
    {
        private readonly IDocumentSignatureService _documentSignatureService = documentSignatureService;
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly ICompanyRepository _companyRepository = companyRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;
        private readonly IBlobService _blobService = blobService;
        private readonly IDocumentService _documentService = documentService;
        private readonly IWebHookManagementService _webHookManagementService = webHookManagementService;
        private readonly IFileDownloadService _fileDownloadService = fileDownloadService;
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;

        public async Task<Guid> GenerateDocumentToSign(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, DateTime dateLimitToSign, 
            int eminderEveryNDays, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId && x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
              ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            if (document.IsAwaitingSignatureDocumentUnit(documentUnitId))
                throw new DomainException(this, DomainErrors.Document.DocumentAlreadySentToSignature(documentUnitId));

            if (document.IsPendingDocumentUnit(documentUnitId) == false)
                throw new DomainException(this, DomainErrors.Document.IsNotPending());

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == document.DocumentTemplateId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            if (documentTemplate.IsSignable == false)
                throw new DomainException(this, DomainErrors.Document.NotSignable(documentUnitId));

            if (documentTemplate.TemplateFileInfo is null)
                throw new DomainException(this, DomainErrors.Document.DocumentNotHaveTemplate(documentId));

            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == employeeId && x.CompanyId == companyId, cancellation: cancellationToken)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), employeeId.ToString()));

            var company = await _companyRepository.FirstOrDefaultAsync(x => x.Id == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Company), companyId.ToString()));

            var documentBytes = await _documentService.GeneratePdf(documentUnitId, documentId, employeeId, companyId, cancellationToken);

            using MemoryStream stream = new(documentBytes);

            document.MarkAsAwaitingDocumentUnitSignature(documentUnitId);

            await SendToSignature(stream, documentUnitId, document, company, employee, documentTemplate.PlaceSignatures.ToArray(), dateLimitToSign, eminderEveryNDays, cancellationToken);

            return documentUnitId;                                                     
        }

        public async Task<Guid> InsertDocumentToSign(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, Domain.AggregatesModel.DocumentAggregate.Extension extension, Stream stream,
            DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId
            && x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            if(document.IsPendingDocumentUnit(documentUnitId) == false)
                throw new DomainException(this, DomainErrors.Document.IsNotPending());

            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == employeeId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), employeeId.ToString()));

            var company = await _companyRepository.FirstOrDefaultAsync(x => x.Id == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Company), companyId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == document.DocumentTemplateId 
            && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            if(documentTemplate.IsSignable == false)
                throw new DomainException(this, DomainErrors.Document.NotSignable(documentUnitId));

            document.MarkAsAwaitingDocumentUnitSignature(documentUnitId);

            await SendToSignature(stream, documentUnitId, document, company, employee, 
                documentTemplate.PlaceSignatures.ToArray() ?? [], dateLimitToSign, eminderEveryNDays, cancellationToken);

            return documentUnitId;
        }

        public async Task<string> ReceiveWebhookDocument(JsonNode contentBody, CancellationToken cancellationToken = default)
        {          
            var webhookEvent = await _webHookManagementService.ParseWebhookEvent(contentBody, cancellationToken);

            if (webhookEvent == null)
                return "O contentBody recebido está vaziu.";

            var document = await _documentRepository.FirstOrDefaultAsync(x => x.DocumentsUnits.Any(x => x.Id == webhookEvent.DocumentUnitId), include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), webhookEvent.DocumentUnitId.ToString()));

            if (document.IsAwaitingSignatureDocumentUnit(webhookEvent.DocumentUnitId) == false)
                return $"O documentUnit {webhookEvent.DocumentUnitId}, não está aguardando assinatura";

            if (webhookEvent.Status == WebhookDocumentStatus.DocRefused 
                || webhookEvent.Status == WebhookDocumentStatus.DocDeleted
                || webhookEvent.Status == WebhookDocumentStatus.DocExpired)
            {
                document.MarkAsInvalidDocumentUnit(webhookEvent.DocumentUnitId);
                var documentUnitId = Guid.NewGuid();
                document.NewDocumentUnit(documentUnitId);
                return $"O status do documentUnit {webhookEvent.DocumentUnitId}, foi alterado com sucesso para INVALID.";
            }

            if (webhookEvent.Status == WebhookDocumentStatus.DocSigned)
            {
                var file = await _fileDownloadService.DownloadFileFromUrlAsync(webhookEvent.Url, cancellationToken);

                string fileNameWithExtesion = document.InsertUnitWithoutRequireValidation(webhookEvent.DocumentUnitId, Guid.NewGuid().ToString(), file.FileExtension);

                await _blobService.UploadAsync(file.FileStream, fileNameWithExtesion, document.CompanyId.ToString(), overwrite: false, cancellationToken: cancellationToken);

                return $"O status do documentUnit {webhookEvent.DocumentUnitId}, foi alterado com sucesso para OK.";
            }

            return "O status não é valido.";
            
        }

        private async Task SendToSignature(Stream stream, Guid documentUnitId, Document document, Company company,
            Employee employee, PlaceSignature[] placeSignatures, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default)
        {
            var documentSigningOptions = employee.DocumentSigningOptions;
            if (documentSigningOptions == DocumentSigningOptions.DigitalSignatureAndWhatsapp)
            {
                await _documentSignatureService.SendToSignatureWithWhatsapp(stream, documentUnitId, document, company, employee,
                placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);
                
                _backgroundJobClient.Schedule<ISignDocumentService>(
                    x => x.InvalidateUnsignedDocument(documentUnitId, document.Id, company.Id, cancellationToken),
                    dateLimitToSign.AddDays(1));
                
                return;
            }

            if(documentSigningOptions == DocumentSigningOptions.DigitalSignatureAndSelfie)
            {
                await _documentSignatureService.SendToSignatureWithSelfie(stream, documentUnitId, document, company, employee,
                placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);
                
                _backgroundJobClient.Schedule<ISignDocumentService>(
                    x => x.InvalidateUnsignedDocument(documentUnitId, document.Id, company.Id, cancellationToken),
                    dateLimitToSign.AddDays(1));
                
                return;
            }

            if (documentSigningOptions == DocumentSigningOptions.DigitalSignatureAndSMS)
            {
                await _documentSignatureService.SendToSignatureWithSMS(stream, documentUnitId, document, company, employee,
                placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);

                _backgroundJobClient.Schedule<ISignDocumentService>(
                    x => x.InvalidateUnsignedDocument(documentUnitId, document.Id, company.Id, cancellationToken),
                    dateLimitToSign.AddDays(1));

                return;
            }

            throw new DomainException(this, DomainErrors.Employee.InvalidDocumentDigitalSigningOptions(employee.Id));
        }

        public async Task InvalidateUnsignedDocument(Guid documentUnitId, Guid documentId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(
                x => x.Id == documentId && x.CompanyId == companyId,
                include: x => x.Include(y => y.DocumentsUnits),
                cancellation: cancellationToken);

            if (document is null)
                return;

            if (document.IsAwaitingSignatureDocumentUnit(documentUnitId))
            {
                document.MarkAsInvalidDocumentUnit(documentUnitId);
                await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

     
    }
}
