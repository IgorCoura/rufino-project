using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

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
            // Mesma razão da ExpirationPolicy: carga zerada não distribui hora nenhuma, então a regra
            // não existe. Ausência é expressa não compondo a policy, não compondo-a com zero.
            if (workload <= TimeSpan.Zero)
                throw new DomainException(nameof(WorkloadPolicy),
                    DomainErrors.DocumentTemplate.PolicyDurationMustBePositive(nameof(WorkloadPolicy), workload));

            Workload = workload;
        }
    }
}
