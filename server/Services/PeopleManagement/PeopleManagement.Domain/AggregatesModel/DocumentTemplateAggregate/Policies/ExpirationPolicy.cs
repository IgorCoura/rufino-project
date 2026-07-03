namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    /// <summary>
    /// Vencimento com renovação indefinida — comportamento atual do template ("vence sempre").
    /// A Fase 3 introduzirá uma variante limitada (vence N vezes) ao lado desta.
    /// </summary>
    public sealed class ExpirationPolicy : IExpirationPolicy
    {
        public TimeSpan Duration { get; }

        public ExpirationPolicy(TimeSpan duration)
        {
            Duration = duration;
        }

        public bool CanRenew(int renewalCount) => true;
    }
}
