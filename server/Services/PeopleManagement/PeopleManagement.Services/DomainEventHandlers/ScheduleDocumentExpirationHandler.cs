using Hangfire;
using MediatR;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;

namespace PeopleManagement.Services.DomainEventHandlers
{


    public class ScheduleDocumentExpirationHandler(IBackgroundJobClient backgroundJobClient) : INotificationHandler<ScheduleDocumentExpirationEvent>
    {
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
        public Task Handle(ScheduleDocumentExpirationEvent notification, CancellationToken cancellationToken)
        {
            _backgroundJobClient.Schedule<IDocumentDepreciationService>(x => x.DepreciateExpirateDocument(notification.DocumentUnitId, 
                notification.DocumentId, notification.CompanyId, cancellationToken), new DateTimeOffset(notification.Expiration, new TimeOnly(3, 0), TimeSpan.Zero));

            return Task.CompletedTask;
        }
    }
}
