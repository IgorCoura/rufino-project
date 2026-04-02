namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.WorkloadCalendar
{
    public record WorkloadPeriod(DateOnly StartDate, DateOnly EndDate);

    public record WorkloadFitResult(bool CanFit, DateOnly WorkloadEndDate, DateOnly? SuggestedStartDate);
}
