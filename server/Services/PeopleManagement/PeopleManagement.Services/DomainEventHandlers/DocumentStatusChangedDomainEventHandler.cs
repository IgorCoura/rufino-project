using MediatR;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Services.Services;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class DocumentStatusChangedDomainEventHandler(
        ILogger<DocumentStatusChangedDomainEventHandler> logger,
        IEmployeeRepository employeeRepository,
        IEmployeeDocumentStatusRefresher statusRefresher) : INotificationHandler<DocumentStatusChangedDomainEvent>
    {
        private readonly ILogger<DocumentStatusChangedDomainEventHandler> _logger = logger;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly IEmployeeDocumentStatusRefresher _statusRefresher = statusRefresher;

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

            try
            {
                await _statusRefresher.RefreshAsync(notification.EmployeeId, notification.CompanyId, cancellationToken);
                await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating employee document status. EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                    notification.EmployeeId,
                    notification.CompanyId);
                throw;
            }
        }
    }
}
