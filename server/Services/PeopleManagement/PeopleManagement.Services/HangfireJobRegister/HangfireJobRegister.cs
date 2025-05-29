using Hangfire;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;

namespace PeopleManagement.Services.HangfireJobRegistrar
{
    public class HangfireJobRegister(IRecurringJobManager recurringJobManager)
    {
        private readonly IRecurringJobManager _recurringJobManager = recurringJobManager;
        public void RegisterRecurringJobs()
        {

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "january-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.JanuaryEvent, CancellationToken.None),
                "0 0 1 1 *"); // Cron expression for January 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "february-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.FebruaryEvent, CancellationToken.None),
                "0 0 1 2 *"); // Cron expression for February 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "march-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.MarchEvent, CancellationToken.None),
                "0 0 1 3 *"); // Cron expression for March 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "april-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.AprilEvent, CancellationToken.None),
                "0 0 1 4 *"); // Cron expression for April 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "may-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.MayEvent, CancellationToken.None),
                "0 0 1 5 *"); // Cron expression for May 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "june-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.JuneEvent, CancellationToken.None),
                "0 0 1 6 *"); // Cron expression for June 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "july-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.JulyEvent, CancellationToken.None),
                "0 0 1 7 *"); // Cron expression for July 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "august-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.AugustEvent, CancellationToken.None),
                "0 0 1 8 *"); // Cron expression for August 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "september-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.SeptemberEvent, CancellationToken.None),
                "0 0 1 9 *"); // Cron expression for September 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "october-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.OctoberEvent, CancellationToken.None),
                "0 0 1 10 *"); // Cron expression for October 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "november-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.NovemberEvent, CancellationToken.None),
                "0 0 1 11 *"); // Cron expression for November 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
                "december-job",
                x => x.RecurringCreateDocumentUnits(RecurringEvents.DecemberEvent, CancellationToken.None),
                "0 0 1 12 *"); // Cron expression for December 1st at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
               "daily-job",
               x => x.RecurringCreateDocumentUnits(RecurringEvents.DailyEvent, CancellationToken.None),
               Cron.Daily); // Cron expression for daily at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
               "weekly-job",
               x => x.RecurringCreateDocumentUnits(RecurringEvents.WeeklyEvent, CancellationToken.None),
               Cron.Weekly); // Cron expression for weekly at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
               "monthly-job",
               x => x.RecurringCreateDocumentUnits(RecurringEvents.MonthlyEvent, CancellationToken.None),
               Cron.Monthly); // Cron expression for monthly at midnight  

            _recurringJobManager.AddOrUpdate<IRecurringDocumentService>(
               "yearly-job",
               x => x.RecurringCreateDocumentUnits(RecurringEvents.YearlyEvent, CancellationToken.None),
               Cron.Yearly); // Cron expression for yearly at midnight
        }
    }
}
