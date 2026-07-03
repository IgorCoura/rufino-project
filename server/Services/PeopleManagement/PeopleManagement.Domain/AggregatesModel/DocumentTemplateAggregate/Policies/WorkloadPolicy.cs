namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    /// <summary>
    /// Carga horária associada ao documento — distribuída em dias úteis pelo WorkloadCalendarService.
    /// </summary>
    public sealed class WorkloadPolicy : IWorkloadPolicy
    {
        public TimeSpan Workload { get; }

        public WorkloadPolicy(TimeSpan workload)
        {
            Workload = workload;
        }
    }
}
