namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.WorkloadCalendar
{
    public interface IHolidayProvider
    {
        bool IsHoliday(DateOnly date);
        HashSet<DateOnly> GetHolidays(int year);
    }
}
