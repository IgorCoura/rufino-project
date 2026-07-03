using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    /// <summary>
    /// Competência: o documento é gerado por período. Carrega a granularidade (PeriodType) e se usa a
    /// competência anterior. Presença desta policy no template = documento por competência; ausência = não é.
    /// </summary>
    public sealed class PeriodPolicy : IPeriodPolicy
    {
        public PeriodType PeriodType { get; }
        public bool UsePreviousPeriod { get; }

        public PeriodPolicy(PeriodType periodType, bool usePreviousPeriod)
        {
            PeriodType = periodType;
            UsePreviousPeriod = usePreviousPeriod;
        }
    }
}
