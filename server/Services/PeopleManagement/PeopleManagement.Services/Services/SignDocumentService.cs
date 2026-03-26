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

using PeopleManagement.Domain.Services;

namespace PeopleManagement.Services.Services
{
    public class SignDocumentService(IDocumentSignatureService documentSignatureService, IDocumentRepository documentRepository, 
        ICompanyRepository companyRepository, IEmployeeRepository employeeRepository, IDocumentTemplateRepository documentTemplateRepository, 
        IBlobService blobService,IDocumentService documentService , IWebHookManagementService webHookManagementService, IFileDownloadService fileDownloadService,
        IBackgroundJobClient backgroundJobClient, IDocumentSignatureReminderService documentSignatureReminderService, IWhatsAppQueueService whatsAppQueueService) : ISignDocumentService
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
        private readonly IDocumentSignatureReminderService _documentSignatureReminderService = documentSignatureReminderService;
        private readonly IWhatsAppQueueService _whatsAppQueueService = whatsAppQueueService;

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
                return "O contentBody recebido está vazio.";

            var document = await _documentRepository.FirstOrDefaultAsync(x => x.DocumentsUnits.Any(x => x.Id == webhookEvent.DocumentUnitId), include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), webhookEvent.DocumentUnitId.ToString()));

            if (document.IsAwaitingSignatureDocumentUnit(webhookEvent.DocumentUnitId) == false)
                return $"O documentUnit {webhookEvent.DocumentUnitId}, não está aguardando assinatura";

            var primaryUnit = document.DocumentsUnits.FirstOrDefault(x => x.Id == webhookEvent.DocumentUnitId)!;

            if (webhookEvent.Status == WebhookDocumentStatus.DocRefused
                || webhookEvent.Status == WebhookDocumentStatus.DocDeleted
                || webhookEvent.Status == WebhookDocumentStatus.DocExpired)
            {
                await InvalidateSessionDocuments(primaryUnit, document, cancellationToken);
                return $"O status do documentUnit {webhookEvent.DocumentUnitId}, foi alterado com sucesso para INVALID.";
            }

            if (webhookEvent.Status == WebhookDocumentStatus.DocSigned)
            {
                var sessionDocUnits = await GetAllSessionDocumentUnits(primaryUnit, document, cancellationToken);
                var processedFiles = new List<(string Url, string FileName)>();

                // Processa documento principal
                var primaryFile = await _fileDownloadService.DownloadFileFromUrlAsync(webhookEvent.Url, cancellationToken);
                var primaryFileName = document.InsertUnitWithoutRequireValidation(webhookEvent.DocumentUnitId, Guid.NewGuid().ToString(), primaryFile.FileExtension);
                await _blobService.UploadAsync(primaryFile.FileStream, primaryFileName, document.CompanyId.ToString(), overwrite: false, cancellationToken: cancellationToken);
                processedFiles.Add((webhookEvent.Url, primaryFileName));

                // Processa extra_docs (attachments da sessão)
                if (webhookEvent.ExtraDocs != null && webhookEvent.ExtraDocs.Count > 0)
                {
                    foreach (var extraDoc in webhookEvent.ExtraDocs)
                    {
                        var attachmentUnit = sessionDocUnits
                            .FirstOrDefault(x => x.Unit.AttachmentToken == extraDoc.Token);

                        if (attachmentUnit.Unit == null)
                            continue;

                        var attachmentFile = await _fileDownloadService.DownloadFileFromUrlAsync(extraDoc.SignedFileUrl, cancellationToken);
                        var attachmentFileName = attachmentUnit.Document.InsertUnitWithoutRequireValidation(
                            attachmentUnit.Unit.Id, Guid.NewGuid().ToString(), attachmentFile.FileExtension);
                        await _blobService.UploadAsync(attachmentFile.FileStream, attachmentFileName,
                            attachmentUnit.Document.CompanyId.ToString(), overwrite: false, cancellationToken: cancellationToken);
                        processedFiles.Add((extraDoc.SignedFileUrl, attachmentFileName));
                    }
                }

                await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

                var employee = await _employeeRepository.FirstOrDefaultAsync(
                    x => x.Id == document.EmployeeId && x.CompanyId == document.CompanyId,
                    cancellation: cancellationToken);

                if (employee?.Contact?.CellPhone != null && !string.IsNullOrWhiteSpace(employee.Contact.CellPhone))
                {
                    try
                    {
                        foreach (var (url, fileName) in processedFiles)
                        {
                            var caption = processedFiles.Count == 1
                                ? $"📄 Olá {employee.Name.FirstName}!\n\nSeu documento foi assinado com sucesso.\nSegue anexo o arquivo assinado para sua consulta."
                                : $"📄 Olá {employee.Name.FirstName}!\n\nSegue anexo um dos seus documentos assinados.";

                            _whatsAppQueueService.EnqueueMediaMessage(
                                phoneNumber: employee.Contact.GetCellPhoneWithCoutryNumber(),
                                mediaType: "document",
                                mimeType: "application/pdf",
                                caption: caption,
                                media: url,
                                fileName: fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao enviar documento via WhatsApp: {ex.Message}");
                    }

                    _backgroundJobClient.Enqueue(() =>
                        _documentSignatureReminderService.SendConsolidatedSignatureReminders(employee.Id, CancellationToken.None));
                }

                return $"O status do documentUnit {webhookEvent.DocumentUnitId}, foi alterado com sucesso para OK.";
            }

            return "O status não é valido.";
        }

        private async Task<List<(DocumentUnit Unit, Document Document)>> GetAllSessionDocumentUnits(
            DocumentUnit primaryUnit, Document primaryDocument, CancellationToken cancellationToken)
        {
            var result = new List<(DocumentUnit Unit, Document Document)>();

            if (string.IsNullOrEmpty(primaryUnit.SignatureDocumentToken))
            {
                result.Add((primaryUnit, primaryDocument));
                return result;
            }

            // Find all documents that have units with the same SignatureDocumentToken
            var allDocuments = await _documentRepository.GetDataAsync(
                filter: x => x.DocumentsUnits.Any(du => du.SignatureDocumentToken == primaryUnit.SignatureDocumentToken),
                include: x => x.Include(y => y.DocumentsUnits),
                cancellation: cancellationToken);

            foreach (var doc in allDocuments)
            {
                foreach (var unit in doc.DocumentsUnits)
                {
                    if (unit.SignatureDocumentToken == primaryUnit.SignatureDocumentToken
                        && unit.Status == DocumentUnitStatus.AwaitingSignature)
                    {
                        result.Add((unit, doc));
                    }
                }
            }

            return result;
        }

        private async Task InvalidateSessionDocuments(DocumentUnit primaryUnit, Document primaryDocument, CancellationToken cancellationToken)
        {
            var sessionDocUnits = await GetAllSessionDocumentUnits(primaryUnit, primaryDocument, cancellationToken);

            foreach (var (unit, doc) in sessionDocUnits)
            {
                doc.MarkAsInvalidDocumentUnit(unit.Id);
                var newUnitId = Guid.NewGuid();
                doc.NewDocumentUnit(newUnitId);
            }

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task SendToSignature(Stream stream, Guid documentUnitId, Document document, Company company,
            Employee employee, PlaceSignature[] placeSignatures, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default)
        {
            var documentUnit = document.DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            var activeSession = await FindActiveSessionForEmployee(employee.Id, document.CompanyId, cancellationToken);

            if (activeSession != null)
            {
                await AttachToExistingSession(stream, documentUnit, document, activeSession, placeSignatures, cancellationToken);
            }
            else
            {
                await CreateNewSigningSession(stream, documentUnitId, documentUnit, document, company, employee, placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);

                _backgroundJobClient.Enqueue(() =>
                    _documentSignatureReminderService.SendImmediateSignatureNotification(documentUnitId, employee.Id, CancellationToken.None));
            }
        }

        private async Task<ActiveSessionInfo?> FindActiveSessionForEmployee(Guid employeeId, Guid companyId, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")));

            var documentsWithActiveSession = await _documentRepository.GetDataAsync(
                filter: x => x.EmployeeId == employeeId && x.CompanyId == companyId
                    && x.DocumentsUnits.Any(du => du.Status == DocumentUnitStatus.AwaitingSignature
                        && du.SignatureDocumentToken != null
                        && du.AttachmentToken == null
                        && du.Date == today),
                include: x => x.Include(y => y.DocumentsUnits),
                cancellation: cancellationToken);

            var candidateUnits = documentsWithActiveSession
                .SelectMany(d => d.DocumentsUnits)
                .Where(du => du.SignatureDocumentToken != null
                    && du.AttachmentToken == null
                    && du.Date == today)
                .ToList();

            if (candidateUnits.Count == 0)
                return null;

            var distinctTokens = candidateUnits
                .Select(du => du.SignatureDocumentToken!)
                .Distinct()
                .ToList();

            // Single query to count attachments for all candidate tokens
            var docsWithAttachments = await _documentRepository.GetDataAsync(
                filter: x => x.CompanyId == companyId
                    && x.DocumentsUnits.Any(du => distinctTokens.Contains(du.SignatureDocumentToken!)
                        && du.AttachmentToken != null),
                include: x => x.Include(y => y.DocumentsUnits),
                cancellation: cancellationToken);

            var attachmentCountByToken = docsWithAttachments
                .SelectMany(d => d.DocumentsUnits)
                .Where(du => du.SignatureDocumentToken != null && du.AttachmentToken != null)
                .GroupBy(du => du.SignatureDocumentToken!)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var docUnit in candidateUnits)
            {
                var attachmentCount = attachmentCountByToken.GetValueOrDefault(docUnit.SignatureDocumentToken!, 0);

                if (attachmentCount < 14)
                {
                    return new ActiveSessionInfo(
                        docUnit.SignatureDocumentToken!,
                        docUnit.SignatureUrl!,
                        docUnit.Id,
                        attachmentCount);
                }
            }

            return null;
        }

        private async Task AttachToExistingSession(Stream stream, DocumentUnit documentUnit, Document document,
            ActiveSessionInfo session, PlaceSignature[] placeSignatures, CancellationToken cancellationToken)
        {
            var attachmentResult = await _documentSignatureService.AddDocumentAttachment(
                session.PrimaryDocToken, stream, document.Name.ToString(), cancellationToken);

            if (placeSignatures.Length > 0)
            {
                var sessionInfo = await _documentSignatureService.GetSessionSignedDocuments(session.PrimaryDocToken, cancellationToken);

                if (!string.IsNullOrEmpty(sessionInfo.SignerToken))
                {
                    await _documentSignatureService.PlaceSignatureOnAttachment(
                        attachmentResult.AttachmentToken, sessionInfo.SignerToken, placeSignatures, cancellationToken);
                }
            }

            documentUnit.SetAttachmentSignatureInfo(session.PrimaryDocToken, attachmentResult.AttachmentToken, session.SignerUrl);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task CreateNewSigningSession(Stream stream, Guid documentUnitId, DocumentUnit documentUnit, Document document,
            Company company, Employee employee, PlaceSignature[] placeSignatures, DateTime dateLimitToSign, int eminderEveryNDays,
            CancellationToken cancellationToken)
        {
            DocumentSignatureModel result;
            var documentSigningOptions = employee.DocumentSigningOptions;

            result = documentSigningOptions switch
            {
                _ when documentSigningOptions == DocumentSigningOptions.DigitalSignatureAndWhatsapp =>
                    await _documentSignatureService.SendToSignatureWithWhatsapp(stream, documentUnitId, document, company, employee,
                        placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken),

                _ when documentSigningOptions == DocumentSigningOptions.DigitalSignatureAndSelfie =>
                    await _documentSignatureService.SendToSignatureWithSelfie(stream, documentUnitId, document, company, employee,
                        placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken),

                _ when documentSigningOptions == DocumentSigningOptions.DigitalSignatureAndSMS =>
                    await _documentSignatureService.SendToSignatureWithSMS(stream, documentUnitId, document, company, employee,
                        placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken),

                _ when documentSigningOptions == DocumentSigningOptions.OnlyWhatsapp =>
                    await _documentSignatureService.SendToSignatureWithOnlyWhatsapp(stream, documentUnitId, document, company, employee,
                        placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken),

                _ when documentSigningOptions == DocumentSigningOptions.OnlySMS =>
                    await _documentSignatureService.SendToSignatureWithOnlySMS(stream, documentUnitId, document, company, employee,
                        placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken),

                _ => throw new DomainException(this, DomainErrors.Employee.InvalidDocumentDigitalSigningOptions(employee.Id))
            };

            documentUnit.SetSignatureInfo(result.DocumentToken, result.SignerUrl);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            _backgroundJobClient.Schedule<ISignDocumentService>(
                x => x.InvalidateUnsignedDocument(documentUnitId, document.Id, company.Id, cancellationToken),
                dateLimitToSign.AddDays(1));
        }

        private record ActiveSessionInfo(string PrimaryDocToken, string SignerUrl, Guid PrimaryDocumentUnitId, int AttachmentCount);

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
                var primaryUnit = document.DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId);

                if (primaryUnit?.SignatureDocumentToken != null && primaryUnit.AttachmentToken == null)
                {
                    // This is a primary document — invalidate all documents in the session
                    await InvalidateSessionDocuments(primaryUnit, document, cancellationToken);
                }
                else
                {
                    document.MarkAsInvalidDocumentUnit(documentUnitId);
                    await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
        }

     
    }
}
