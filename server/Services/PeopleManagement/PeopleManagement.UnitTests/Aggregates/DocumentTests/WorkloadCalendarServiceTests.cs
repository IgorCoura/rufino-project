using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.WorkloadCalendar;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    public class WorkloadCalendarServiceTests
    {
        private readonly WorkloadCalendarService _service;
        private readonly BrazilianHolidayProvider _holidayProvider;

        public WorkloadCalendarServiceTests()
        {
            _holidayProvider = new BrazilianHolidayProvider();
            _service = new WorkloadCalendarService(_holidayProvider);
        }

        [Fact]
        public void IsWorkingDay_Weekday_ReturnsTrue()
        {
            // 2026-04-06 is Monday
            var date = new DateOnly(2026, 4, 6);
            Assert.True(_service.IsWorkingDay(date));
        }

        [Fact]
        public void IsWorkingDay_Saturday_ReturnsFalse()
        {
            // 2026-04-04 is Saturday
            var date = new DateOnly(2026, 4, 4);
            Assert.False(_service.IsWorkingDay(date));
        }

        [Fact]
        public void IsWorkingDay_Sunday_ReturnsFalse()
        {
            // 2026-04-05 is Sunday
            var date = new DateOnly(2026, 4, 5);
            Assert.False(_service.IsWorkingDay(date));
        }

        [Fact]
        public void IsWorkingDay_Holiday_ReturnsFalse()
        {
            // 2026-04-21 is Tiradentes
            var date = new DateOnly(2026, 4, 21);
            Assert.False(_service.IsWorkingDay(date));
        }

        [Fact]
        public void IsWorkingDay_GoodFriday2026_ReturnsFalse()
        {
            // Easter 2026 is April 5, Good Friday is April 3
            var date = new DateOnly(2026, 4, 3);
            Assert.False(_service.IsWorkingDay(date));
        }

        [Fact]
        public void IsWorkingDay_Carnival2026_ReturnsFalse()
        {
            // Easter 2026 is April 5, Carnival Tuesday is Easter - 46 = Feb 18
            var date = new DateOnly(2026, 2, 17); // Carnival Monday (Easter - 47)
            Assert.False(_service.IsWorkingDay(date));
        }

        [Fact]
        public void GetNextWorkingDay_FromFriday_ReturnsMonday()
        {
            // 2026-04-10 is Friday
            var date = new DateOnly(2026, 4, 10);
            var next = _service.GetNextWorkingDay(date);
            Assert.Equal(new DateOnly(2026, 4, 13), next); // Monday
        }

        [Fact]
        public void DistributeWorkload_8Hours_8MaxPerDay_Returns1Day()
        {
            // Monday 2026-04-06
            var startDate = new DateOnly(2026, 4, 6);
            var result = _service.DistributeWorkload(startDate, TimeSpan.FromHours(8), 8);

            Assert.Equal(startDate, result.StartDate);
            Assert.Equal(startDate, result.EndDate);
        }

        [Fact]
        public void DistributeWorkload_40Hours_8MaxPerDay_Returns5WorkingDays()
        {
            // Monday 2026-04-06
            var startDate = new DateOnly(2026, 4, 6);
            var result = _service.DistributeWorkload(startDate, TimeSpan.FromHours(40), 8);

            Assert.Equal(startDate, result.StartDate);
            // Should be Friday 2026-04-10 (5 working days Mon-Fri)
            Assert.Equal(new DateOnly(2026, 4, 10), result.EndDate);
        }

        [Fact]
        public void DistributeWorkload_40Hours_StartingThursday_SkipsWeekend()
        {
            // Thursday 2026-04-09
            var startDate = new DateOnly(2026, 4, 10);
            var result = _service.DistributeWorkload(startDate, TimeSpan.FromHours(40), 8);

            // Thu(8) + Fri(8) + [skip Sat/Sun] + Mon(8) + Tue(8) + Wed(8) = 40h
            // EndDate should be Wednesday 2026-04-15
            Assert.Equal(new DateOnly(2026, 4, 16), result.EndDate);
        }

        [Fact]
        public void DistributeWorkload_SkipsHoliday()
        {
            // 2026-04-20 is Monday, 2026-04-21 is Tiradentes (holiday)
            var startDate = new DateOnly(2026, 4, 20);
            var result = _service.DistributeWorkload(startDate, TimeSpan.FromHours(16), 8);

            // Mon(8) + [skip Tue holiday] + Wed(8) = 16h
            Assert.Equal(new DateOnly(2026, 4, 22), result.EndDate);
        }

        [Fact]
        public void TryFitWorkload_NoExistingUsage_Fits()
        {
            var startDate = new DateOnly(2026, 4, 6); // Monday
            var existing = new Dictionary<DateOnly, TimeSpan>();

            var result = _service.TryFitWorkload(startDate, TimeSpan.FromHours(8), 8, existing);

            Assert.True(result.CanFit);
            Assert.Equal(startDate, result.WorkloadEndDate);
            Assert.Null(result.SuggestedStartDate);
        }

        [Fact]
        public void TryFitWorkload_PartialDay_6Plus2_Fits()
        {
            var startDate = new DateOnly(2026, 4, 6); // Monday
            var existing = new Dictionary<DateOnly, TimeSpan>
            {
                { startDate, TimeSpan.FromHours(6) }
            };

            var result = _service.TryFitWorkload(startDate, TimeSpan.FromHours(2), 8, existing);

            Assert.True(result.CanFit);
            Assert.Equal(startDate, result.WorkloadEndDate);
        }

        [Fact]
        public void TryFitWorkload_PartialDay_6Plus4_SpillsToNextDay()
        {
            var startDate = new DateOnly(2026, 4, 6); // Monday
            var existing = new Dictionary<DateOnly, TimeSpan>
            {
                { startDate, TimeSpan.FromHours(6) }
            };

            // 4h new: 2h fits on Monday (6+2=8), remaining 2h goes to Tuesday
            var result = _service.TryFitWorkload(startDate, TimeSpan.FromHours(4), 8, existing);

            Assert.True(result.CanFit);
            Assert.Equal(new DateOnly(2026, 4, 7), result.WorkloadEndDate); // Tuesday
        }

        [Fact]
        public void TryFitWorkload_FullDay_CannotFit_SuggestsNextDate()
        {
            var startDate = new DateOnly(2026, 4, 6); // Monday
            var existing = new Dictionary<DateOnly, TimeSpan>
            {
                { startDate, TimeSpan.FromHours(8) }
            };

            var result = _service.TryFitWorkload(startDate, TimeSpan.FromHours(4), 8, existing);

            Assert.False(result.CanFit);
            Assert.NotNull(result.SuggestedStartDate);
            Assert.Equal(new DateOnly(2026, 4, 7), result.SuggestedStartDate); // Tuesday
        }

        [Fact]
        public void TryFitWorkload_MultipleDaysFull_SuggestsFirstAvailable()
        {
            var startDate = new DateOnly(2026, 4, 6); // Monday
            var existing = new Dictionary<DateOnly, TimeSpan>
            {
                { new DateOnly(2026, 4, 6), TimeSpan.FromHours(8) },
                { new DateOnly(2026, 4, 7), TimeSpan.FromHours(8) },
            };

            var result = _service.TryFitWorkload(startDate, TimeSpan.FromHours(8), 8, existing);

            Assert.False(result.CanFit);
            Assert.Equal(new DateOnly(2026, 4, 8), result.SuggestedStartDate); // Wednesday
        }

        [Fact]
        public void BrazilianHolidays_2026_ContainsFixedHolidays()
        {
            var holidays = _holidayProvider.GetHolidays(2026);

            Assert.Contains(new DateOnly(2026, 1, 1), holidays);   // Confraternização Universal
            Assert.Contains(new DateOnly(2026, 4, 21), holidays);  // Tiradentes
            Assert.Contains(new DateOnly(2026, 5, 1), holidays);   // Dia do Trabalho
            Assert.Contains(new DateOnly(2026, 9, 7), holidays);   // Independência
            Assert.Contains(new DateOnly(2026, 10, 12), holidays); // Nossa Senhora Aparecida
            Assert.Contains(new DateOnly(2026, 11, 2), holidays);  // Finados
            Assert.Contains(new DateOnly(2026, 11, 15), holidays); // Proclamação da República
            Assert.Contains(new DateOnly(2026, 12, 25), holidays); // Natal
        }

        [Fact]
        public void BrazilianHolidays_2026_ContainsMovableHolidays()
        {
            var holidays = _holidayProvider.GetHolidays(2026);

            // Easter 2026 is April 5
            Assert.Contains(new DateOnly(2026, 2, 17), holidays);  // Carnival Monday (Easter - 47)
            Assert.Contains(new DateOnly(2026, 2, 18), holidays);  // Carnival Tuesday (Easter - 46)
            Assert.Contains(new DateOnly(2026, 4, 3), holidays);   // Good Friday (Easter - 2)
            Assert.Contains(new DateOnly(2026, 6, 4), holidays);   // Corpus Christi (Easter + 60)
        }

        [Fact]
        public void BrazilianHolidays_CachesPerYear()
        {
            var holidays1 = _holidayProvider.GetHolidays(2026);
            var holidays2 = _holidayProvider.GetHolidays(2026);

            Assert.Same(holidays1, holidays2);
        }

        [Fact]
        public void VerifyTimeConflict_40Hours_StartingFriday10April_EndsThursday16April()
        {
            // Arrange
            var startDate = new DateOnly(2026, 4, 10); // Friday
            var workload = TimeSpan.FromHours(40);
            var maxHoursPerDay = 8;

            // Act — Step 1: Verify start date is a working day
            Assert.True(_service.IsWorkingDay(startDate));

            // Act — Step 2: Distribute workload across working days
            var projectedPeriod = _service.DistributeWorkload(startDate, workload, maxHoursPerDay);

            // Assert — Distribution spans from Fri 10/04 to Thu 16/04
            // Fri 10(8h) + [Sat skip] + [Sun skip] + Mon 13(8h) + Tue 14(8h) + Wed 15(8h) + Thu 16(8h) = 40h
            Assert.Equal(startDate, projectedPeriod.StartDate);
            Assert.Equal(new DateOnly(2026, 4, 16), projectedPeriod.EndDate);

            // Act — Step 3: Try to fit with no existing conflicts
            var noConflicts = new Dictionary<DateOnly, TimeSpan>();
            var fitResult = _service.TryFitWorkload(startDate, workload, maxHoursPerDay, noConflicts);

            // Assert — Fits perfectly, end date matches distribution
            Assert.True(fitResult.CanFit);
            Assert.Equal(new DateOnly(2026, 4, 16), fitResult.WorkloadEndDate);
            Assert.Null(fitResult.SuggestedStartDate);
        }

        [Fact]
        public void VerifyTimeConflict_40Hours_StartingFriday10April_EachDayAllocates8Hours()
        {
            // Arrange
            var startDate = new DateOnly(2026, 4, 10); // Friday
            var workload = TimeSpan.FromHours(40);
            var maxHoursPerDay = 8;

            // Verify each day in the expected range
            var expectedWorkingDays = new[]
            {
                new DateOnly(2026, 4, 10), // Friday  — 8h
                new DateOnly(2026, 4, 13), // Monday  — 8h
                new DateOnly(2026, 4, 14), // Tuesday — 8h
                new DateOnly(2026, 4, 15), // Wednesday — 8h
                new DateOnly(2026, 4, 16), // Thursday  — 8h
            };

            var skippedDays = new[]
            {
                new DateOnly(2026, 4, 11), // Saturday
                new DateOnly(2026, 4, 12), // Sunday
            };

            foreach (var day in expectedWorkingDays)
                Assert.True(_service.IsWorkingDay(day), $"{day} should be a working day");

            foreach (var day in skippedDays)
                Assert.False(_service.IsWorkingDay(day), $"{day} should NOT be a working day");

            // Distribute and verify total coverage is exactly 5 working days × 8h = 40h
            var period = _service.DistributeWorkload(startDate, workload, maxHoursPerDay);
            Assert.Equal(expectedWorkingDays.First(), period.StartDate);
            Assert.Equal(expectedWorkingDays.Last(), period.EndDate);
        }

        [Fact]
        public void VerifyTimeConflict_40Hours_StartingFriday10April_WithPartialConflictOnFriday_StillFits()
        {
            // Arrange — Another document already uses 4h on Friday 10/04
            var startDate = new DateOnly(2026, 4, 10);
            var workload = TimeSpan.FromHours(40);
            var maxHoursPerDay = 8;

            var existingUsage = new Dictionary<DateOnly, TimeSpan>
            {
                { new DateOnly(2026, 4, 10), TimeSpan.FromHours(4) } // 4h already used on Friday
            };

            // Act
            var fitResult = _service.TryFitWorkload(startDate, workload, maxHoursPerDay, existingUsage);

            // Assert — Still fits, but spills further because only 4h available on Friday
            // Fri 10(4h) + Mon 13(8h) + Tue 14(8h) + Wed 15(8h) + Thu 16(8h) + Fri 17(4h) = 40h
            Assert.True(fitResult.CanFit);
            Assert.Equal(new DateOnly(2026, 4, 17), fitResult.WorkloadEndDate);
        }

        [Fact]
        public void VerifyTimeConflict_40Hours_StartingFriday10April_WithFullConflictOnFriday_CannotFit()
        {
            // Arrange — Friday 10/04 is fully booked (8h)
            var startDate = new DateOnly(2026, 4, 10);
            var workload = TimeSpan.FromHours(40);
            var maxHoursPerDay = 8;

            var existingUsage = new Dictionary<DateOnly, TimeSpan>
            {
                { new DateOnly(2026, 4, 10), TimeSpan.FromHours(8) }
            };

            // Act
            var fitResult = _service.TryFitWorkload(startDate, workload, maxHoursPerDay, existingUsage);

            // Assert — Cannot fit, suggests Monday 13/04
            Assert.False(fitResult.CanFit);
            Assert.Equal(new DateOnly(2026, 4, 13), fitResult.SuggestedStartDate);
        }

        [Fact]
        public void VerifyTimeConflict_40Hours_StartingFriday10April_WithConflictsOnMultipleDays_SpillsAccordingly()
        {
            // Arrange — Existing documents use partial hours on several days
            var startDate = new DateOnly(2026, 4, 10);
            var workload = TimeSpan.FromHours(40);
            var maxHoursPerDay = 8;

            var existingUsage = new Dictionary<DateOnly, TimeSpan>
            {
                { new DateOnly(2026, 4, 10), TimeSpan.FromHours(4) }, // Fri: 4h used, 4h available
                { new DateOnly(2026, 4, 13), TimeSpan.FromHours(4) }, // Mon: 4h used, 4h available
                { new DateOnly(2026, 4, 14), TimeSpan.FromHours(8) }, // Tue: fully booked
            };

            // Act
            var fitResult = _service.TryFitWorkload(startDate, workload, maxHoursPerDay, existingUsage);

            // Assert — Fits but spills:
            // Fri 10: 4h available → allocate 4h (remaining 36h)
            // Mon 13: 4h available → allocate 4h (remaining 32h)
            // Tue 14: 0h available → skip
            // Wed 15: 8h available → allocate 8h (remaining 24h)
            // Thu 16: 8h available → allocate 8h (remaining 16h)
            // Fri 17: 8h available → allocate 8h (remaining 8h)
            // [Sat/Sun skip]
            // Mon 20: 8h available → allocate 8h (remaining 0h)
            Assert.True(fitResult.CanFit);
            Assert.Equal(new DateOnly(2026, 4, 20), fitResult.WorkloadEndDate);
        }

        [Fact]
        public void VerifyTimeConflict_40Hours_OnWeekend_IsNotWorkingDay()
        {
            // 2026-04-11 is Saturday, 2026-04-12 is Sunday
            Assert.False(_service.IsWorkingDay(new DateOnly(2026, 4, 11)));
            Assert.False(_service.IsWorkingDay(new DateOnly(2026, 4, 12)));

            // Next working day after Saturday should be Monday 13/04
            Assert.Equal(new DateOnly(2026, 4, 13), _service.GetNextWorkingDay(new DateOnly(2026, 4, 11)));
        }
    }
}
