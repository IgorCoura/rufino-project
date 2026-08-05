using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.Options;

namespace PeopleManagement.Services.DomainEventHandlers
{
    /// <summary>
    /// Traduz o agendamento gravado no documento num job atrasado. O domínio não conhece o Hangfire — mesma
    /// divisão do <see cref="ScheduleDocumentExpirationHandler"/>.
    /// </summary>
    public class ScheduleDocumentSignatureSendHandler(IBackgroundJobClient backgroundJobClient,
        ILogger<ScheduleDocumentSignatureSendHandler> logger, TimeZoneOptions timeZoneOptions)
        : INotificationHandler<ScheduleDocumentSignatureSendEvent>
    {
        // 09:00 no fuso da empresa. O disparo notifica o funcionário (WhatsApp/SMS), diferente da depreciação,
        // que roda de madrugada porque ninguém é avisado. Os lembretes de assinatura já usam 12:00/19:00 locais.
        private static readonly TimeOnly SendAtLocalTime = new(9, 0);

        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
        private readonly ILogger<ScheduleDocumentSignatureSendHandler> _logger = logger;
        private readonly TimeZoneOptions _timeZoneOptions = timeZoneOptions;

        public Task Handle(ScheduleDocumentSignatureSendEvent notification, CancellationToken cancellationToken)
        {
            var runAt = ToLocalSendMoment(notification.SendOn);

            _logger.LogInformation(
                "Scheduling signature send for unit {DocumentUnitId} of document {DocumentId} at {RunAt}.",
                notification.DocumentUnitId, notification.DocumentId, runAt);

            _backgroundJobClient.Schedule<ISignDocumentService>(
                x => x.SendScheduledDocumentToSign(notification.DocumentUnitId, notification.DocumentId,
                    notification.CompanyId, notification.SendOn, cancellationToken),
                runAt);

            return Task.CompletedTask;
        }

        // Agendar para hoje já com o horário passado faria o Hangfire disparar imediatamente — que é o
        // comportamento certo: a data pedida é hoje, e o envio não deve esperar até amanhã.
        private DateTimeOffset ToLocalSendMoment(DateOnly sendOn)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneOptions.TimeZoneId);
            var localMoment = sendOn.ToDateTime(SendAtLocalTime);

            return new DateTimeOffset(localMoment, timeZone.GetUtcOffset(localMoment));
        }
    }
}
