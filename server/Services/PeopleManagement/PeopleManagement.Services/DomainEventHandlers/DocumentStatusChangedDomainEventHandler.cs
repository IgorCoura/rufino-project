using MediatR;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.Services;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class DocumentStatusChangedDomainEventHandler : INotificationHandler<DocumentStatusChangedDomainEvent>
    {
        private readonly ILogger<DocumentStatusChangedDomainEventHandler> _logger;
        private readonly IDocumentRepository _documentRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmployeeDocumentStatusService _employeeDocumentStatusService;

        public DocumentStatusChangedDomainEventHandler(
            ILogger<DocumentStatusChangedDomainEventHandler> logger,
            IDocumentRepository documentRepository,
            IEmployeeRepository employeeRepository,
            IEmployeeDocumentStatusService employeeDocumentStatusService)
        {
            _logger = logger;
            _documentRepository = documentRepository;
            _employeeRepository = employeeRepository;
            _employeeDocumentStatusService = employeeDocumentStatusService;
        }

        public async Task Handle(DocumentStatusChangedDomainEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Document status changed. DocumentId: {DocumentId}, EmployeeId: {EmployeeId}, CompanyId: {CompanyId}, OldStatus: {OldStatus} ({OldStatusId}), NewStatus: {NewStatus} ({NewStatusId})",
                notification.DocumentId,
                notification.EmployeeId,
                notification.CompanyId,
                notification.OldStatus.Name,
                notification.OldStatus.Id,
                notification.NewStatus.Name,
                notification.NewStatus.Id
            );

            // Atualiza o status agregado do Employee
            await UpdateEmployeeDocumentStatusAsync(notification.EmployeeId, notification.CompanyId, cancellationToken);
        }

        private async Task UpdateEmployeeDocumentStatusAsync(Guid employeeId, Guid companyId, CancellationToken cancellationToken)
        {
            try
            {
                // Busca todos os IDs de status dos documentos do funcionário
                var documentStatusIds = await _documentRepository.GetAllStatusByEmployeeAsync(employeeId, companyId, cancellationToken);

                // Busca o funcionário
                var employee = await _employeeRepository.FirstOrDefaultAsync(
                    x => x.Id == employeeId && x.CompanyId == companyId,
                    cancellation: cancellationToken);

                if (employee == null)
                {
                    _logger.LogWarning(
                        "Employee not found when trying to update document status. EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                        employeeId,
                        companyId);
                    return;
                }

                // Usa o serviço de domínio para determinar o status do Employee
                var newStatus = _employeeDocumentStatusService.DetermineStatusFromDocumentStatuses(documentStatusIds);

                // Atualiza o status do funcionário
                employee.UpdateDocumentRepresentingStatus(newStatus);

                // Salva as mudanças
                await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Employee document representing status updated. EmployeeId: {EmployeeId}, NewStatus: {NewStatus}",
                    employeeId,
                    employee.DocumentRepresentingStatus.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating employee document status. EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                    employeeId,
                    companyId);
                throw;
            }
        }
    }
}
