using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.Services;
using PeopleManagement.Services.Services;

namespace PeopleManagement.Services.HangfireJobRegistrar
{
    public class HangfireJobRegister(
        IRecurringJobManager recurringJobManager, 
        IBackgroundJobClient backgroundJobClient, 
        IWebHostEnvironment environment,
        ILogger<HangfireJobRegister> logger) 
    {
        private readonly IRecurringJobManager _recurringJobManager = recurringJobManager;
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly ILogger<HangfireJobRegister> _logger = logger;
        public void RegisterRecurringJobs()
        {
            _logger.LogInformation("Register Recurring Jobs Init");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "january-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.JANUARY, CancellationToken.None),
                "0 0 1 1 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "february-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.FEBRUARY, CancellationToken.None),
                "0 0 1 2 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "march-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.MARCH, CancellationToken.None),
                "0 0 1 3 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "april-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.APRIL, CancellationToken.None),
                "0 0 1 4 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "may-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.MAY, CancellationToken.None),
                "0 0 1 5 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "june-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.JUNE, CancellationToken.None),
                "0 0 1 6 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "july-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.JULY, CancellationToken.None),
                "0 0 1 7 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "august-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.AUGUST, CancellationToken.None),
                "0 0 1 8 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "september-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.SEPTEMBER, CancellationToken.None),
                "0 0 1 9 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "october-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.OCTOBER, CancellationToken.None),
                "0 0 1 10 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "november-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.NOVEMBER, CancellationToken.None),
                "0 0 1 11 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "december-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.DECEMBER, CancellationToken.None),
                "0 0 1 12 *");

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
               "daily-job",
               x => x.RecurringCreateDocumentUnits(RecurringEvents.DAILY, CancellationToken.None),
               Cron.Daily);

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
               "weekly-job",
               x => x.RecurringCreateDocumentUnits(RecurringEvents.WEEKLY, CancellationToken.None),
               Cron.Weekly);

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
               "monthly-job",
               x => x.RecurringCreateDocumentUnits(RecurringEvents.MONTHLY, CancellationToken.None),
               Cron.Monthly);

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
               "yearly-job",
               x => x.RecurringCreateDocumentUnits(RecurringEvents.YEARLY, CancellationToken.None),
               Cron.Yearly);

            // Configure the HTTP request pipeline.
            if (_environment != null && _environment.Equals("Development"))
            {
                _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
               "minutely-job",
               x => x.RecurringCreateDocumentUnits(RecurringEvents.MINUTELY, CancellationToken.None),
               Cron.Minutely);
            }

            _recurringJobManager.AddOrUpdate<IWebHookManagementService>(
               "refresh-webhook-job",
               x => x.RefreshWebHookEvent(CancellationToken.None),
               Cron.Daily);

            _backgroundJobClient.Enqueue<IWebHookManagementService>(x => x.RefreshWebHookEvent(CancellationToken.None));

            RecurringJob.AddOrUpdate<IDocumentSignatureReminderService>(
                "signature-reminders-morning",
                service => service.SendConsolidatedSignatureReminders(CancellationToken.None),
                "0 12 * * *",
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")
                });

            RecurringJob.AddOrUpdate<IDocumentSignatureReminderService>(
                "signature-reminders-afternoon",
                service => service.SendConsolidatedSignatureReminders(CancellationToken.None),
                "0 19 * * *",
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")
                });

            RecurringJob.AddOrUpdate<IWhatsAppHealthCheckService>(
                "signature-reminders-morning",
                service => service.SendHealthCheckMessage(CancellationToken.None),
                "0 12 * * *",
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")
                });

            RecurringJob.AddOrUpdate<IWhatsAppHealthCheckService>(
                "signature-reminders-afternoon",
                service => service.SendHealthCheckMessage(CancellationToken.None),
                "0 19 * * *",
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")
                });

            _logger.LogInformation("Register Recurring Jobs Complete");
        }
    }
}
