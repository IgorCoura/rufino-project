namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.WorkloadCalendar
{
    public interface IWorkloadCalendarService
    {
        bool IsWorkingDay(DateOnly date);
        DateOnly GetNextWorkingDay(DateOnly date);
        WorkloadPeriod DistributeWorkload(DateOnly startDate, TimeSpan totalWorkload, int maxHoursPerDay);
        WorkloadFitResult TryFitWorkload(DateOnly startDate, TimeSpan totalWorkload, int maxHoursPerDay, Dictionary<DateOnly, TimeSpan> existingUsage);
    }
}
