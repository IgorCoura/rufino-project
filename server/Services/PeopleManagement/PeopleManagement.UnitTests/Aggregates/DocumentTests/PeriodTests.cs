using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    /// <summary>
    /// Caracterização do value object <see cref="Period"/> — o núcleo da regra de "competência".
    /// Fixa o comportamento atual de <see cref="Period.Create"/> (período corrente) e
    /// <see cref="Period.CreatePreviousPeriod"/> (período anterior) para cada <see cref="PeriodType"/>,
    /// antes de a lógica migrar para uma PeriodPolicy no template.
    /// </summary>
    public class PeriodTests
    {
        private static readonly DateTime MidYear = new(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        // -----------------------------------------------------------------
        // Create: período corrente por tipo.
        // -----------------------------------------------------------------

        [Fact]
        public void Create_Daily_BuildsCurrentDay()
        {
            var period = Period.Create(PeriodType.Daily, MidYear);

            Assert.True(period.IsDaily);
            Assert.Equal(2024, period.Year);
            Assert.Equal(6, period.Month);
            Assert.Equal(15, period.Day);
        }

        [Fact]
        public void Create_Monthly_BuildsCurrentMonth()
        {
            var period = Period.Create(PeriodType.Monthly, MidYear);

            Assert.True(period.IsMonthly);
            Assert.Equal(2024, period.Year);
            Assert.Equal(6, period.Month);
            Assert.Null(period.Day);
        }

        [Fact]
        public void Create_Yearly_BuildsCurrentYear()
        {
            var period = Period.Create(PeriodType.Yearly, MidYear);

            Assert.True(period.IsYearly);
            Assert.Equal(2024, period.Year);
        }

        [Fact]
        public void Create_Weekly_BuildsCurrentWeek()
        {
            var period = Period.Create(PeriodType.Weekly, MidYear);

            Assert.True(period.IsWeekly);
            Assert.Equal(2024, period.Year);
            Assert.NotNull(period.Week);
        }

        // -----------------------------------------------------------------
        // CreatePreviousPeriod: período anterior por tipo.
        // -----------------------------------------------------------------

        [Fact]
        public void CreatePreviousPeriod_Daily_ReturnsPreviousDay()
        {
            var period = Period.CreatePreviousPeriod(PeriodType.Daily, MidYear);

            Assert.Equal(2024, period.Year);
            Assert.Equal(6, period.Month);
            Assert.Equal(14, period.Day);
        }

        [Fact]
        public void CreatePreviousPeriod_Monthly_ReturnsPreviousMonth()
        {
            var period = Period.CreatePreviousPeriod(PeriodType.Monthly, MidYear);

            Assert.Equal(2024, period.Year);
            Assert.Equal(5, period.Month);
        }

        [Fact]
        public void CreatePreviousPeriod_Yearly_ReturnsPreviousYear()
        {
            var period = Period.CreatePreviousPeriod(PeriodType.Yearly, MidYear);

            Assert.Equal(2023, period.Year);
        }

        [Fact]
        public void CreatePreviousPeriod_Weekly_ReturnsEarlierWeek()
        {
            var current = Period.Create(PeriodType.Weekly, MidYear);
            var previous = Period.CreatePreviousPeriod(PeriodType.Weekly, MidYear);

            Assert.Equal(current.Week!.Value - 1, previous.Week!.Value);
        }

        // -----------------------------------------------------------------
        // Bordas de virada de mês/ano.
        // -----------------------------------------------------------------

        [Fact]
        public void CreatePreviousPeriod_MonthlyOnJanuary_RollsToDecemberPreviousYear()
        {
            var january = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

            var period = Period.CreatePreviousPeriod(PeriodType.Monthly, january);

            Assert.Equal(2023, period.Year);
            Assert.Equal(12, period.Month);
        }

        [Fact]
        public void CreatePreviousPeriod_DailyOnFirstOfMarchLeapYear_RollsToFeb29()
        {
            var march1 = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            var period = Period.CreatePreviousPeriod(PeriodType.Daily, march1);

            Assert.Equal(2024, period.Year);
            Assert.Equal(2, period.Month);
            Assert.Equal(29, period.Day);
        }

        // -----------------------------------------------------------------
        // Igualdade de value object (crítica para o dedup de DocumentUnit por período).
        // -----------------------------------------------------------------

        [Fact]
        public void Equals_SameTypeAndDate_AreEqual()
        {
            var a = Period.Create(PeriodType.Monthly, MidYear);
            var b = Period.Create(PeriodType.Monthly, MidYear);

            Assert.Equal(a, b);
        }

        [Fact]
        public void Equals_DifferentMonth_AreNotEqual()
        {
            var a = Period.Create(PeriodType.Monthly, MidYear);
            var b = Period.Create(PeriodType.Monthly, MidYear.AddMonths(1));

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Equals_SameYearMonthButDifferentType_AreNotEqual()
        {
            var monthly = Period.Create(PeriodType.Monthly, MidYear);
            var yearly = Period.Create(PeriodType.Yearly, MidYear);

            Assert.NotEqual(monthly, yearly);
        }

        // -----------------------------------------------------------------
        // GetDate: só o diário resolve para uma data concreta.
        // -----------------------------------------------------------------

        [Fact]
        public void GetDate_Daily_ReturnsConcreteDate()
        {
            var period = Period.Create(PeriodType.Daily, MidYear);

            Assert.Equal(new DateOnly(2024, 6, 15), period.GetDate());
        }

        [Fact]
        public void GetDate_Monthly_ReturnsNull()
        {
            var period = Period.Create(PeriodType.Monthly, MidYear);

            Assert.Null(period.GetDate());
        }

        // -----------------------------------------------------------------
        // Guardas de validação (fixa os limites atuais das factories).
        // -----------------------------------------------------------------

        [Fact]
        public void CreateDaily_InvalidYear_Throws()
            => Assert.Throws<DomainException>(() => Period.CreateDaily(1899, 6, 15));

        [Fact]
        public void CreateMonthly_InvalidMonth_Throws()
            => Assert.Throws<DomainException>(() => Period.CreateMonthly(2024, 13));

        [Fact]
        public void CreateWeekly_InvalidWeek_Throws()
            => Assert.Throws<DomainException>(() => Period.CreateWeekly(2024, 6, 54));
    }
}
