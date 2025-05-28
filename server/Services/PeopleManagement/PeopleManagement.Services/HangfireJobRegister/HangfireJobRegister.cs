using Hangfire;
using System.Diagnostics;

namespace PeopleManagement.Services.HangfireJobRegistrar
{
    public  class HangfireJobRegister(IRecurringJobManager recurringJobManager)
    {
        private readonly IRecurringJobManager _recurringJobManager = recurringJobManager;
        public void RegisterRecurringJobs()
        {

            _recurringJobManager.AddOrUpdate(
                "monthly-job",
                () => Debug.WriteLine("Monthly job executed."),
                Cron.Minutely);
        }
    }
}
