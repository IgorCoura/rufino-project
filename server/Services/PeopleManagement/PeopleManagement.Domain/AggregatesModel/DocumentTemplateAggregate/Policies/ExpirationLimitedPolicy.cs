using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    /// <summary>
    /// Vencimento com validade limitada — o documento vence no máximo <see cref="MaxRenewals"/> vezes e depois
    /// para de vencer. Coexiste com <see cref="ExpirationPolicy"/> (vence sempre) sob a mesma capacidade
    /// <see cref="IExpirationPolicy"/>; quem decide é o consumidor, via <see cref="HasValidityCycleLeft"/>.
    ///
    /// O teto NÃO impede renovar. Esgotados os ciclos, o RH continua podendo renovar, substituir, depreciar e
    /// invalidar — as unidades novas é que passam a nascer sem validade, como num template sem esta regra. Um
    /// teto que recusava a renovação deixava o documento vencido sem saída nenhuma: não dava para renovar, e a
    /// unidade vencida não é invalidável (é prova do período coberto).
    /// </summary>
    public sealed class ExpirationLimitedPolicy : IExpirationPolicy
    {
        public TimeSpan Duration { get; }

        /// <summary>Quantos ciclos de validade o documento admite antes de parar de vencer.</summary>
        public int MaxRenewals { get; }

        public ExpirationLimitedPolicy(TimeSpan duration, int maxRenewals)
        {
            // Mesma invariante da ExpirationPolicy: duração zerada é ausência disfarçada de regra.
            if (duration <= TimeSpan.Zero)
                throw new DomainException(nameof(ExpirationLimitedPolicy),
                    DomainErrors.DocumentTemplate.PolicyDurationMustBePositive(nameof(ExpirationLimitedPolicy), duration));

            // Um limite de zero não é "limitado a zero" — é um documento que já nasce sem vencer, o que não é o
            // propósito desta regra. Para "não vence", basta não ter policy de vencimento.
            if (maxRenewals < 1)
                throw new DomainException(nameof(ExpirationLimitedPolicy),
                    DomainErrors.DocumentTemplate.PolicyMaxRenewalsMustBePositive(maxRenewals));

            Duration = duration;
            MaxRenewals = maxRenewals;
        }

        // renewalCount = quantos ciclos de validade o documento já gastou. Enquanto não atingiu o teto, a próxima
        // unidade ainda nasce com validade; atingido, ela nasce sem — e o documento para de vencer.
        public bool HasValidityCycleLeft(int renewalCount) => renewalCount < MaxRenewals;
    }
}
