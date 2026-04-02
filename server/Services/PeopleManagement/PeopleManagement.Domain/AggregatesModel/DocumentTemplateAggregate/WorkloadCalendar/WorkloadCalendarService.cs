namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.WorkloadCalendar
{
    public class WorkloadCalendarService(IHolidayProvider holidayProvider) : IWorkloadCalendarService
    {
        private readonly IHolidayProvider _holidayProvider = holidayProvider;

        public bool IsWorkingDay(DateOnly date)
        {
            return date.DayOfWeek != DayOfWeek.Saturday
                && date.DayOfWeek != DayOfWeek.Sunday
                && !_holidayProvider.IsHoliday(date);
        }

        public DateOnly GetNextWorkingDay(DateOnly date)
        {
            date = date.AddDays(1);
            while (!IsWorkingDay(date))
                date = date.AddDays(1);
            return date;
        }

        public WorkloadPeriod DistributeWorkload(DateOnly startDate, TimeSpan totalWorkload, int maxHoursPerDay)
        {
            var remainingHours = totalWorkload.TotalHours;
            var currentDate = startDate;
            var endDate = startDate;

            while (remainingHours > 0)
            {
                if (!IsWorkingDay(currentDate))
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                var hoursToAllocate = Math.Min(remainingHours, maxHoursPerDay);
                remainingHours -= hoursToAllocate;
                endDate = currentDate;
                currentDate = currentDate.AddDays(1);
            }

            return new WorkloadPeriod(startDate, endDate);
        }

        public WorkloadFitResult TryFitWorkload(DateOnly startDate, TimeSpan totalWorkload, int maxHoursPerDay, Dictionary<DateOnly, TimeSpan> existingUsage)
        {
            var usedOnStartDate = existingUsage.TryGetValue(startDate, out var startUsed) ? startUsed.TotalHours : 0;
            if (usedOnStartDate >= maxHoursPerDay)
            {
                var suggestedDate = FindNextAvailableDate(startDate.AddDays(1), maxHoursPerDay, existingUsage);
                return new WorkloadFitResult(false, startDate, suggestedDate);
            }

            var remainingHours = totalWorkload.TotalHours;
            var currentDate = startDate;
            var endDate = startDate;

            while (remainingHours > 0)
            {
                if (!IsWorkingDay(currentDate))
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                var usedHours = existingUsage.TryGetValue(currentDate, out var used) ? used.TotalHours : 0;
                var availableHours = maxHoursPerDay - usedHours;

                if (availableHours <= 0)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                var hoursToAllocate = Math.Min(remainingHours, availableHours);
                remainingHours -= hoursToAllocate;
                endDate = currentDate;
                currentDate = currentDate.AddDays(1);
            }

            return new WorkloadFitResult(true, endDate, null);
        }

        private DateOnly FindNextAvailableDate(DateOnly fromDate, int maxHoursPerDay, Dictionary<DateOnly, TimeSpan> existingUsage)
        {
            var candidate = fromDate;
            while (true)
            {
                if (!IsWorkingDay(candidate))
                {
                    candidate = candidate.AddDays(1);
                    continue;
                }

                var usedHours = existingUsage.TryGetValue(candidate, out var used) ? used.TotalHours : 0;
                if (maxHoursPerDay - usedHours > 0)
                    return candidate;

                candidate = candidate.AddDays(1);
            }
        }
    }
}
