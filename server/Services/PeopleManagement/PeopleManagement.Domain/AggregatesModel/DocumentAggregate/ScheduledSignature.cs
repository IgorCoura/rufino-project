using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    /// <summary>
    /// Envio para assinatura agendado para uma data futura: a unidade só sai para o funcionário assinar em
    /// <see cref="SendOn"/>, e não no momento em que o RH pediu.
    ///
    /// Presença do VO na unidade = envio agendado; ausência = nada agendado. Os três valores viajam juntos
    /// (e não como campos soltos na unidade) porque só fazem sentido em conjunto — data de envio sem prazo de
    /// assinatura seria um agendamento que o disparo não consegue executar.
    /// </summary>
    public sealed class ScheduledSignature : ValueObject
    {
        public DateOnly SendOn { get; private set; }
        public DateOnly DateLimitToSign { get; private set; }
        public int ReminderEveryNDays { get; private set; }

        private ScheduledSignature() { }

        private ScheduledSignature(DateOnly sendOn, DateOnly dateLimitToSign, int reminderEveryNDays)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Agendar para o passado é um envio que nunca acontece: o job nasceria com data vencida e dispararia
            // de imediato, exatamente o oposto do que o agendamento existe para fazer.
            if (sendOn < today)
                throw new DomainException(this, DomainErrors.Document.ScheduleSendDateInPast(sendOn, today));

            // O prazo é contado a partir do envio. Um prazo anterior (ou igual) nasceria vencido, e a unidade
            // seria invalidada pelo InvalidateUnsignedDocument antes de o funcionário ter chance de assinar.
            if (dateLimitToSign <= sendOn)
                throw new DomainException(this, DomainErrors.Document.ScheduleDateLimitBeforeSendDate(dateLimitToSign, sendOn));

            SendOn = sendOn;
            DateLimitToSign = dateLimitToSign;
            ReminderEveryNDays = reminderEveryNDays;
        }

        public static ScheduledSignature Create(DateOnly sendOn, DateOnly dateLimitToSign, int reminderEveryNDays = 0)
            => new(sendOn, dateLimitToSign, reminderEveryNDays);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return SendOn;
            yield return DateLimitToSign;
            yield return ReminderEveryNDays;
        }
    }
}
