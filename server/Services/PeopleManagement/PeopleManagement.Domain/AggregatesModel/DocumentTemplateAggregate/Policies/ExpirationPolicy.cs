using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

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
            // Presença da policy = regra ativa. Uma duração zerada seria uma regra que não vence nada —
            // ausência disfarçada de presença. A policy simplesmente não pode existir nesse estado.
            if (duration <= TimeSpan.Zero)
                throw new DomainException(nameof(ExpirationPolicy),
                    DomainErrors.DocumentTemplate.PolicyDurationMustBePositive(nameof(ExpirationPolicy), duration));

            Duration = duration;
        }

        public bool CanRenew(int renewalCount) => true;
    }
}
