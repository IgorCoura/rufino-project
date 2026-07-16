using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.UnitTests.Aggregates.DocumentTests.Mothers;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    /// <summary>
    /// Caracterização do cálculo de validade (vencimento) da <see cref="DocumentUnit"/> e da emissão
    /// do <see cref="ScheduleDocumentExpirationEvent"/>, antes de a duração migrar para uma ExpirationPolicy.
    ///
    /// Nota: o setter de <see cref="DocumentUnit.Validity"/> acopla-se ao relógio real (compara com
    /// DateTime.UtcNow), então a validade precisa ser futura. Usamos "Today" como base determinística
    /// dentro da execução — não é tempo aleatório, apenas o hoje exigido pelo próprio SUT.
    /// </summary>
    public class DocumentUnitExpirationTests
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
        private static readonly DateTime FixedReference = new(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void UpdateDetails_WithTimeSpanDuration_SetsValidityAsDatePlusDuration()
        {
            var document = DocumentMother.Simple();
            var unit = document.NewDocumentUnit(Guid.NewGuid());

            document.UpdateDocumentUnitDetails(unit.Id, Today, TimeSpan.FromDays(30), "");

            Assert.Equal(Today.AddDays(30), unit.Validity);
        }

        [Fact]
        public void UpdateDetails_WithZeroDuration_LeavesValidityNull()
        {
            var document = DocumentMother.Simple();
            var unit = document.NewDocumentUnit(Guid.NewGuid());

            document.UpdateDocumentUnitDetails(unit.Id, Today, TimeSpan.Zero, "");

            Assert.Null(unit.Validity);
        }

        [Fact]
        public void UpdateDetails_WithNullDuration_LeavesValidityNull()
        {
            var document = DocumentMother.Simple();
            var unit = document.NewDocumentUnit(Guid.NewGuid());

            document.UpdateDocumentUnitDetails(unit.Id, Today, (TimeSpan?)null, "");

            Assert.Null(unit.Validity);
        }

        [Fact]
        public void UpdateDetails_WhenResultingValidityIsInThePast_Throws()
        {
            var document = DocumentMother.Simple();
            var unit = document.NewDocumentUnit(Guid.NewGuid());

            Assert.Throws<DomainException>(() =>
                document.UpdateDocumentUnitDetails(unit.Id, Today.AddDays(-60), TimeSpan.FromDays(30), ""));
        }

        [Fact]
        public void InsertWithoutRequireValidation_WhenValiditySet_EmitsScheduleExpirationEventWithPayload()
        {
            var document = DocumentMother.Simple();
            var unit = document.NewDocumentUnit(Guid.NewGuid());
            document.UpdateDocumentUnitDetails(unit.Id, Today, TimeSpan.FromDays(30), "");

            document.InsertUnitWithoutRequireValidation(unit.Id, "arquivo", "pdf");

            var evt = Assert.Single(unit.DomainEvents.OfType<ScheduleDocumentExpirationEvent>());
            Assert.Equal(document.Id, evt.DocumentId);
            Assert.Equal(unit.Id, evt.DocumentUnitId);
            Assert.Equal(document.CompanyId, evt.CompanyId);
            Assert.Equal(Today.AddDays(30), evt.Expiration);
            Assert.Equal(Today, evt.IssuedAt);
        }

        [Fact]
        public void InsertWithoutRequireValidation_WhenNoValidity_DoesNotEmitScheduleExpirationEvent()
        {
            var document = DocumentMother.Simple();
            var unit = document.NewDocumentUnit(Guid.NewGuid());
            document.UpdateDocumentUnitDetails(unit.Id, Today, TimeSpan.Zero, "");

            document.InsertUnitWithoutRequireValidation(unit.Id, "arquivo", "pdf");

            Assert.Empty(unit.DomainEvents.OfType<ScheduleDocumentExpirationEvent>());
        }

        [Fact]
        public void MarkAsValid_WhenValiditySet_EmitsScheduleExpirationEvent()
        {
            var document = DocumentMother.Simple();
            var unit = document.NewDocumentUnit(Guid.NewGuid());
            document.UpdateDocumentUnitDetails(unit.Id, Today, TimeSpan.FromDays(30), "");
            document.InsertUnitWithRequireValidation(unit.Id, "arquivo", "pdf");

            document.MarkAsValidDocumentUnit(unit.Id);

            Assert.Single(unit.DomainEvents.OfType<ScheduleDocumentExpirationEvent>());
        }

        [Fact]
        public void NewDocumentUnit_WhenUsePreviousPeriodFalse_SetsCurrentPeriod()
        {
            var document = DocumentMother.Simple(usePreviousPeriod: false, periodType: PeriodType.Monthly);

            var unit = document.NewDocumentUnit(Guid.NewGuid(), FixedReference);

            Assert.Equal(2024, unit.Period!.Year);
            Assert.Equal(6, unit.Period.Month);
        }

        [Fact]
        public void NewDocumentUnit_WhenUsePreviousPeriodTrue_SetsPreviousPeriod()
        {
            var document = DocumentMother.Simple(usePreviousPeriod: true, periodType: PeriodType.Monthly);

            var unit = document.NewDocumentUnit(Guid.NewGuid(), FixedReference);

            Assert.Equal(2024, unit.Period!.Year);
            Assert.Equal(5, unit.Period.Month);
        }
    }
}
