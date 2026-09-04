using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    /// <summary>
    /// Vencimento indefinido — o documento vence sempre, ciclo após ciclo, sem teto.
    /// Ao lado dela vive a variante limitada (<see cref="ExpirationLimitedPolicy"/>, vence N vezes).
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

        public bool HasValidityCycleLeft(int renewalCount) => true;
    }
}
