using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    /// <summary>
    /// Vencimento com renovação limitada — o documento vence e renova no máximo <see cref="MaxRenewals"/> vezes,
    /// depois para. Coexiste com <see cref="ExpirationPolicy"/> (renovação indefinida) sob a mesma capacidade
    /// <see cref="IExpirationPolicy"/>; quem decide é o consumidor, via CanRenew.
    /// </summary>
    public sealed class ExpirationLimitedPolicy : IExpirationPolicy
    {
        public TimeSpan Duration { get; }

        /// <summary>Quantas renovações o documento admite antes de parar de renovar.</summary>
        public int MaxRenewals { get; }

        public ExpirationLimitedPolicy(TimeSpan duration, int maxRenewals)
        {
            // Mesma invariante da ExpirationPolicy: duração zerada é ausência disfarçada de regra.
            if (duration <= TimeSpan.Zero)
                throw new DomainException(nameof(ExpirationLimitedPolicy),
                    DomainErrors.DocumentTemplate.PolicyDurationMustBePositive(nameof(ExpirationLimitedPolicy), duration));

            // Um limite de zero renovações não é "limitado a zero" — é um documento que vence e nunca renova,
            // o que não é o propósito desta regra. Para "não vence", basta não ter policy de vencimento.
            if (maxRenewals < 1)
                throw new DomainException(nameof(ExpirationLimitedPolicy),
                    DomainErrors.DocumentTemplate.PolicyMaxRenewalsMustBePositive(maxRenewals));

            Duration = duration;
            MaxRenewals = maxRenewals;
        }

        // renewalCount = quantas vezes já renovou. Ainda pode renovar enquanto não atingiu o teto.
        public bool CanRenew(int renewalCount) => renewalCount < MaxRenewals;
    }
}
