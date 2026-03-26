using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.Services;

namespace PeopleManagement.Services.Services
{
    public interface IDocumentSignatureReminderService
    {
        Task SendConsolidatedSignatureReminders(CancellationToken cancellationToken = default);
        Task SendConsolidatedSignatureReminders(Guid employeeId, CancellationToken cancellationToken = default);
        Task SendImmediateSignatureNotification(Guid documentUnitId, Guid employeeId, CancellationToken cancellationToken = default);
    }

    public class DocumentSignatureReminderService : IDocumentSignatureReminderService
    {
        private readonly IWhatsAppQueueService _whatsAppQueueService;
        private readonly IDocumentRepository _documentRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<DocumentSignatureReminderService> _logger;

        public DocumentSignatureReminderService(
            IWhatsAppQueueService whatsAppQueueService,
            IDocumentRepository documentRepository,
            IEmployeeRepository employeeRepository,
            ILogger<DocumentSignatureReminderService> logger)
        {
            _whatsAppQueueService = whatsAppQueueService;
            _documentRepository = documentRepository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 1800)] // 30 minutos de timeout
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 120, 600 })] // Retry: 2min, 10min
        public async Task SendConsolidatedSignatureReminders(CancellationToken cancellationToken = default)
        {
            await SendConsolidatedSignatureRemindersInternal(null, cancellationToken);
        }

        public async Task SendConsolidatedSignatureReminders(Guid employeeId, CancellationToken cancellationToken = default)
        {
            await SendConsolidatedSignatureRemindersInternal(employeeId, cancellationToken);
        }

        private async Task SendConsolidatedSignatureRemindersInternal(Guid? employeeId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting consolidated signature reminders job");

                IEnumerable<Document> documents = await _documentRepository.GetDataAsync(
                    filter: x => x.DocumentsUnits.Any(du => du.Status == DocumentUnitStatus.AwaitingSignature) 
                                 && (!employeeId.HasValue || x.EmployeeId == employeeId.Value),
                    include: q => q.Include(d => d.DocumentsUnits),
                    cancellation: cancellationToken);

                List<Document> documentsList = documents.ToList();

                if (!documentsList.Any())
                {
                    _logger.LogInformation("No pending documents found for signature reminders");
                    return;
                }

                var pendingDocumentsByEmployee = documentsList
                    .SelectMany(doc => doc.DocumentsUnits
                        .Where(du => du.Status == DocumentUnitStatus.AwaitingSignature && !string.IsNullOrEmpty(du.SignatureUrl))
                        .Select(du => new
                        {
                            EmployeeId = doc.EmployeeId,
                            DocumentName = doc.Name.Value,
                            SignatureUrl = du.SignatureUrl,
                            DocumentUnitId = du.Id
                        }))
                    .GroupBy(x => x.EmployeeId)
                    .ToList();

                _logger.LogInformation("Found {EmployeeCount} employees with pending documents", pendingDocumentsByEmployee.Count);

                foreach (var employeeGroup in pendingDocumentsByEmployee)
                {
                    try
                    {
                        var employee = await _employeeRepository.FirstOrDefaultAsync(
                            x => x.Id == employeeGroup.Key,
                            cancellation: cancellationToken);

                        if (employee == null || employee.Contact == null || string.IsNullOrEmpty(employee.Contact.CellPhone))
                        {
                            _logger.LogWarning("Employee or contact not found for EmployeeId: {EmployeeId}", employeeGroup.Key);
                            continue;
                        }

                        var pendingDocuments = employeeGroup.ToList();
                        var phoneNumber = employee.Contact.GetCellPhoneWithCoutryNumber();

                        var message = BuildConsolidatedMessage(employee.Name, pendingDocuments);

                        _whatsAppQueueService.EnqueueTextMessage(phoneNumber, message);

                        _logger.LogInformation(
                            "Consolidated signature reminder sent to EmployeeId: {EmployeeId}, Documents count: {Count}",
                            employeeGroup.Key,
                            pendingDocuments.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error sending consolidated reminder to EmployeeId: {EmployeeId}",
                            employeeGroup.Key);
                    }
                }

                _logger.LogInformation("Consolidated signature reminders job completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in consolidated signature reminders job");
            }
        }

        public async Task SendImmediateSignatureNotification(Guid documentUnitId, Guid employeeId, CancellationToken cancellationToken = default)
        {
            try
            {
                var document = await _documentRepository.FirstOrDefaultAsync(
                    x => x.DocumentsUnits.Any(du => du.Id == documentUnitId),
                    include: q => q.Include(d => d.DocumentsUnits),
                    cancellation: cancellationToken);

                if (document == null)
                {
                    _logger.LogWarning("Document not found for DocumentUnitId: {DocumentUnitId}", documentUnitId);
                    return;
                }

                var documentUnit = document.DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId);
                if (documentUnit == null || string.IsNullOrEmpty(documentUnit.SignatureUrl))
                {
                    _logger.LogWarning("DocumentUnit or SignatureUrl not found for DocumentUnitId: {DocumentUnitId}", documentUnitId);
                    return;
                }

                var employee = await _employeeRepository.FirstOrDefaultAsync(
                    x => x.Id == employeeId,
                    cancellation: cancellationToken);

                if (employee is null || employee.Contact == null || string.IsNullOrEmpty(employee.Contact.CellPhone))
                {
                    _logger.LogWarning("Employee or contact not found for EmployeeId: {EmployeeId}", employeeId);
                    return;
                }

                var phoneNumber = employee.Contact.GetCellPhoneWithCoutryNumber();

                var message = $"Olá {employee.Name}! 👋\n\n" +
                              $"Você recebeu novos documentos para assinatura.\n\n" +
                              $"Por favor, acesse o link abaixo para assinar:\n" +
                              $"{documentUnit.SignatureUrl}\n\n" +
                              $"Você receberá lembretes periódicos até a assinatura ser concluída.";

                _whatsAppQueueService.EnqueueTextMessage(phoneNumber, message);

                _logger.LogInformation(
                    "Immediate signature notification sent for DocumentUnitId: {DocumentUnitId}, EmployeeId: {EmployeeId}",
                    documentUnitId,
                    employeeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending immediate notification for DocumentUnitId: {DocumentUnitId}, EmployeeId: {EmployeeId}",
                    documentUnitId,
                    employeeId);
            }
        }

        private static string BuildConsolidatedMessage(string employeeName, dynamic pendingDocuments)
        {
            var message = $"Olá {employeeName}! 👋\n\n";

            if (pendingDocuments.Count == 1)
            {
                message += $"Você possui *1 documento* pendente de assinatura:\n\n";
            }
            else
            {
                message += $"Você possui *{pendingDocuments.Count} documentos* pendentes de assinatura:\n\n";
            }

            // Group documents by SignatureUrl to deduplicate links for session-grouped docs
            var groupedByUrl = new Dictionary<string, List<string>>();
            for (int i = 0; i < pendingDocuments.Count; i++)
            {
                var doc = pendingDocuments[i];
                string url = doc.SignatureUrl;
                string name = doc.DocumentName;

                if (!groupedByUrl.ContainsKey(url))
                    groupedByUrl[url] = new List<string>();

                groupedByUrl[url].Add(name);
            }

            int docIndex = 1;
            foreach (var group in groupedByUrl)
            {
                foreach (var docName in group.Value)
                {
                    message += $"📄 *Documento {docIndex}:* {docName}\n";
                    docIndex++;
                }
                message += $"🔗 Link: {group.Key}\n\n";
            }

            message += "⏰ Este é um lembrete automático enviado duas vezes ao dia.\n";
            message += "✅ Assim que você assinar todos os documentos, os lembretes serão interrompidos automaticamente.";

            return message;
        }
    }
}
