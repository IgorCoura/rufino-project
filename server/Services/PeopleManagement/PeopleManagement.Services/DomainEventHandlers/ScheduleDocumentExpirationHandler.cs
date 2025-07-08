using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;

namespace PeopleManagement.Services.DomainEventHandlers
{


    public class ScheduleDocumentExpirationHandler(IBackgroundJobClient backgroundJobClient, ILogger<ScheduleDocumentExpirationHandler> logger, DocumentOptions documentOptions) : INotificationHandler<ScheduleDocumentExpirationEvent>
    {
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
        private readonly ILogger<ScheduleDocumentExpirationHandler> _logger = logger;
        public Task Handle(ScheduleDocumentExpirationEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Schedule document expiration event - Expiration: {notification.Expiration}, DocumentUnit: {notification.DocumentUnitId}");
            _backgroundJobClient.Schedule<IDocumentDepreciationService>(x => x.DepreciateExpirateDocument(notification.DocumentUnitId, 
                notification.DocumentId, notification.CompanyId, cancellationToken), new DateTimeOffset(notification.Expiration, new TimeOnly(3, 0), TimeSpan.Zero));

            _backgroundJobClient.Schedule<IDocumentDepreciationService>(x => x.WarningExpirateDocument(notification.DocumentUnitId,
                notification.DocumentId, notification.CompanyId, cancellationToken), new DateTimeOffset(notification.Expiration.AddDays(documentOptions.WarningDaysBeforeDocumentExpiration * -1), new TimeOnly(3, 0), TimeSpan.Zero));

            return Task.CompletedTask;
        }
    }
}
