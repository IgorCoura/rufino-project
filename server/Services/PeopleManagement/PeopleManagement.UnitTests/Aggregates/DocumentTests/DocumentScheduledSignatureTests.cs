using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.UnitTests.Aggregates.DocumentTests.Mothers;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    /// <summary>
    /// Agendamento do envio para assinatura: o VO <see cref="ScheduledSignature"/> e os métodos de entrada no
    /// <see cref="Document"/>.
    ///
    /// As datas ancoram em "hoje" (e não numa constante de 2024) porque a invariante do VO compara com o dia
    /// corrente — mesma razão pela qual os cenários de vencimento fazem isso.
    /// </summary>
    public class DocumentScheduledSignatureTests
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
        private static readonly DateOnly SendOn = Today.AddDays(30);
        private static readonly DateOnly DateLimitToSign = Today.AddDays(35);

        // A unidade precisa estar pendente e datada — a data oficial é pré-requisito das transições vizinhas.
        private static (Document doc, Guid unitId) DocumentWithPendingUnit()
        {
            var doc = DocumentMother.Simple();
            var unitId = Guid.NewGuid();
            doc.NewDocumentUnit(unitId);
            doc.UpdateDocumentUnitDetails(unitId, Today, TimeSpan.Zero, "");
            return (doc, unitId);
        }

        [Fact]
        public void Create_WithValidDates_ShouldKeepEveryValue()
        {
            var schedule = ScheduledSignature.Create(SendOn, DateLimitToSign, reminderEveryNDays: 3);

            Assert.Equal(SendOn, schedule.SendOn);
            Assert.Equal(DateLimitToSign, schedule.DateLimitToSign);
            Assert.Equal(3, schedule.ReminderEveryNDays);
        }

        // Agendar para hoje é legítimo: o disparo sai hoje mesmo, não é "passado".
        [Fact]
        public void Create_WithSendOnToday_ShouldBeAccepted()
        {
            var schedule = ScheduledSignature.Create(Today, Today.AddDays(5));

            Assert.Equal(Today, schedule.SendOn);
        }

        [Fact]
        public void Create_WithSendOnInThePast_ShouldThrow()
        {
            Assert.Throws<DomainException>(() => ScheduledSignature.Create(Today.AddDays(-1), DateLimitToSign));
        }

        [Theory]
        [InlineData(0)]  // prazo igual ao envio: nasce vencido
        [InlineData(-1)] // prazo antes do envio
        public void Create_WithDateLimitNotAfterSendOn_ShouldThrow(int daysFromSendOn)
        {
            Assert.Throws<DomainException>(() => ScheduledSignature.Create(SendOn, SendOn.AddDays(daysFromSendOn)));
        }

        [Fact]
        public void ScheduleSignatureSend_WithPendingUnit_ShouldRecordTheSchedule()
        {
            var (doc, unitId) = DocumentWithPendingUnit();

            doc.ScheduleSignatureSend(unitId, SendOn, DateLimitToSign, reminderEveryNDays: 0);

            var unit = doc.GetDocumentUnit(unitId);
            Assert.True(unit.IsSignatureScheduled);
            Assert.Equal(SendOn, unit.ScheduledSignature!.SendOn);
            Assert.Equal(DateLimitToSign, unit.ScheduledSignature.DateLimitToSign);
        }

        // O agendamento não muda o estado da unidade: ela segue pendente até o disparo acontecer.
        [Fact]
        public void ScheduleSignatureSend_ShouldLeaveTheUnitPending()
        {
            var (doc, unitId) = DocumentWithPendingUnit();

            doc.ScheduleSignatureSend(unitId, SendOn, DateLimitToSign, reminderEveryNDays: 0);

            Assert.Equal(DocumentUnitStatus.Pending, doc.GetDocumentUnit(unitId).Status);
        }

        [Fact]
        public void ScheduleSignatureSend_ShouldEmitTheEventWithTheChosenDate()
        {
            var (doc, unitId) = DocumentWithPendingUnit();

            doc.ScheduleSignatureSend(unitId, SendOn, DateLimitToSign, reminderEveryNDays: 0);

            var unit = doc.GetDocumentUnit(unitId);
            var scheduleEvent = Assert.Single(unit.DomainEvents.OfType<ScheduleDocumentSignatureSendEvent>());
            Assert.Equal(unitId, scheduleEvent.DocumentUnitId);
            Assert.Equal(doc.Id, scheduleEvent.DocumentId);
            Assert.Equal(doc.CompanyId, scheduleEvent.CompanyId);
            Assert.Equal(SendOn, scheduleEvent.SendOn);
        }

        // Reagendar substitui: é o VO gravado que o disparo consulta, e o job antigo desiste ao ver a data nova.
        [Fact]
        public void ScheduleSignatureSend_CalledTwice_ShouldKeepOnlyTheLatestSchedule()
        {
            var (doc, unitId) = DocumentWithPendingUnit();
            doc.ScheduleSignatureSend(unitId, SendOn, DateLimitToSign, reminderEveryNDays: 0);

            var newSendOn = SendOn.AddDays(10);
            doc.ScheduleSignatureSend(unitId, newSendOn, newSendOn.AddDays(5), reminderEveryNDays: 0);

            Assert.Equal(newSendOn, doc.GetDocumentUnit(unitId).ScheduledSignature!.SendOn);
        }

        [Fact]
        public void ScheduleSignatureSend_WithUnitAwaitingSignature_ShouldThrow()
        {
            var (doc, unitId) = DocumentWithPendingUnit();
            doc.MarkAsAwaitingDocumentUnitSignature(unitId);

            Assert.Throws<DomainException>(() => doc.ScheduleSignatureSend(unitId, SendOn, DateLimitToSign, 0));
        }

        [Fact]
        public void ScheduleSignatureSend_WithDeliveredUnit_ShouldThrow()
        {
            var (doc, unitId) = DocumentWithPendingUnit();
            doc.InsertUnitWithoutRequireValidation(unitId, "arquivo", Extension.PDF);

            Assert.Throws<DomainException>(() => doc.ScheduleSignatureSend(unitId, SendOn, DateLimitToSign, 0));
        }

        [Fact]
        public void ScheduleSignatureSend_WithUnknownUnit_ShouldThrow()
        {
            var (doc, _) = DocumentWithPendingUnit();

            Assert.Throws<DomainException>(() => doc.ScheduleSignatureSend(Guid.NewGuid(), SendOn, DateLimitToSign, 0));
        }

        [Fact]
        public void CancelScheduledSignatureSend_WithSchedule_ShouldClearIt()
        {
            var (doc, unitId) = DocumentWithPendingUnit();
            doc.ScheduleSignatureSend(unitId, SendOn, DateLimitToSign, reminderEveryNDays: 0);

            doc.CancelScheduledSignatureSend(unitId);

            Assert.False(doc.GetDocumentUnit(unitId).IsSignatureScheduled);
        }

        // Cancelar o que já não existe é a mesma intenção realizada — não é erro.
        [Fact]
        public void CancelScheduledSignatureSend_WithoutSchedule_ShouldDoNothing()
        {
            var (doc, unitId) = DocumentWithPendingUnit();

            doc.CancelScheduledSignatureSend(unitId);

            Assert.False(doc.GetDocumentUnit(unitId).IsSignatureScheduled);
        }

        [Fact]
        public void CancelScheduledSignatureSend_WithUnknownUnit_ShouldThrow()
        {
            var (doc, _) = DocumentWithPendingUnit();

            Assert.Throws<DomainException>(() => doc.CancelScheduledSignatureSend(Guid.NewGuid()));
        }
    }
}
