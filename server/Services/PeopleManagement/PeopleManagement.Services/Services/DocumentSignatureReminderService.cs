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
        Task SendImmediateSignatureNotification(Guid documentUnitId, Guid employeeId, CancellationToken cancellationToken = default);
    }

    public class DocumentSignatureReminderService : IDocumentSignatureReminderService
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly IDocumentRepository _documentRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<DocumentSignatureReminderService> _logger;

        public DocumentSignatureReminderService(
            IWhatsAppService whatsAppService,
            IDocumentRepository documentRepository,
            IEmployeeRepository employeeRepository,
            ILogger<DocumentSignatureReminderService> logger)
        {
            _whatsAppService = whatsAppService;
            _documentRepository = documentRepository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task SendConsolidatedSignatureReminders(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting consolidated signature reminders job");

                var documents = await _documentRepository.GetDataAsync(
                    filter: x => x.DocumentsUnits.Any(du => du.Status == DocumentUnitStatus.AwaitingSignature),
                    include: q => q.Include(d => d.DocumentsUnits),
                    cancellation: cancellationToken);

                var documentsList = documents.ToList();

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
                            DocumentName = doc.Name,
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
                        var phoneNumber = employee.Contact.CellPhone;

                        var message = BuildConsolidatedMessage(employee.Name, pendingDocuments);

                        await _whatsAppService.SendTextMessageAsync(phoneNumber, message, cancellationToken);

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

                if (employee == null || employee.Contact == null || string.IsNullOrEmpty(employee.Contact.CellPhone))
                {
                    _logger.LogWarning("Employee or contact not found for EmployeeId: {EmployeeId}", employeeId);
                    return;
                }

                var phoneNumber = employee.Contact.CellPhone;
                var message = $"Olá {employee.Name}! 👋\n\n" +
                              $"Você recebeu um novo documento para assinatura: *{document.Name}*\n\n" +
                              $"Por favor, acesse o link abaixo para assinar:\n" +
                              $"{documentUnit.SignatureUrl}\n\n" +
                              $"Você receberá lembretes periódicos até a assinatura ser concluída.";

                await _whatsAppService.SendTextMessageAsync(phoneNumber, message, cancellationToken);

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

            for (int i = 0; i < pendingDocuments.Count; i++)
            {
                var doc = pendingDocuments[i];
                message += $"📄 *Documento {i + 1}:* {doc.DocumentName}\n";
                message += $"🔗 Link: {doc.SignatureUrl}\n\n";
            }

            message += "⏰ Este é um lembrete automático enviado duas vezes ao dia.\n";
            message += "✅ Assim que você assinar todos os documentos, os lembretes serão interrompidos automaticamente.";

            return message;
        }
    }
}
