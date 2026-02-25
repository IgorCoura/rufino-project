using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Globalization;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class Period : ValueObject
    {
        public PeriodType Type { get; private set; }
        public int? Day { get; private set; }
        public int? Week { get; private set; }
        public int Month { get; private set; }
        public int Year { get; private set; }

        private Period() { }

        private Period(PeriodType type, int year, int month, int? day = null, int? week = null)
        {
            Type = type;
            Year = year;
            Month = month;
            Day = day;
            Week = week;
        }

        public static Period Create(PeriodType type, DateTime date)
        {
            if (type.Equals(PeriodType.Daily))
                return CreateDaily(date.Year, date.Month, date.Day);

            if (type.Equals(PeriodType.Weekly))
                return CreateWeekly(date.Year, date.Month, GetWeekOfYear(date));

            if (type.Equals(PeriodType.Monthly))
                return CreateMonthly(date.Year, date.Month);

            if (type.Equals(PeriodType.Yearly))
                return CreateYearly(date.Year);

            throw new DomainException(nameof(Period), DomainErrors.FieldInvalid(nameof(PeriodType), type.ToString()));
        }

        public static Period CreatePreviousPeriod(PeriodType type, DateTime date)
        {
            if (type.Equals(PeriodType.Daily))
            {
                var previousDay = date.AddDays(-1);
                return CreateDaily(previousDay.Year, previousDay.Month, previousDay.Day);
            }

            if (type.Equals(PeriodType.Weekly))
            {
                var previousWeek = date.AddDays(-7);
                return CreateWeekly(previousWeek.Year, previousWeek.Month, GetWeekOfYear(previousWeek));
            }

            if (type.Equals(PeriodType.Monthly))
            {
                var previousMonth = date.AddMonths(-1);
                return CreateMonthly(previousMonth.Year, previousMonth.Month);
            }

            if (type.Equals(PeriodType.Yearly))
            {
                var previousYear = date.AddYears(-1);
                return CreateYearly(previousYear.Year);
            }

            throw new DomainException(nameof(Period), DomainErrors.FieldInvalid(nameof(PeriodType), type.ToString()));
        }

        public static Period CreateDaily(int year, int month, int day)
        {
            if (year < 1900 || year > 9999)
                throw new DomainException(nameof(Period), DomainErrors.FieldInvalid(nameof(Year), year.ToString()));

            if (month < 1 || month > 12)
                throw new DomainException(nameof(Period), DomainErrors.FieldInvalid(nameof(Month), month.ToString()));

            if (day < 1 || day > 31)
                throw new DomainException(nameof(Period), DomainErrors.FieldInvalid(nameof(Day), day.ToString()));

            try
            {
                var date = new DateOnly(year, month, day);
                return new Period(PeriodType.Daily, year, month, day, null);
            }
            catch
            {
                throw new DomainException(nameof(Period), DomainErrors.FieldInvalid("Date", $"{year}-{month}-{day}"));
            }
        }

        public static Period CreateWeekly(int year, int month, int week)
        {
            if (year < 1900 || year > 9999)
                throw new DomainException(nameof(Period), DomainErrors.FieldInvalid(nameof(Year), year.ToString()));

            if (week < 1 || week > 53)
                throw new DomainException(nameof(Period), DomainErrors.FieldInvalid(nameof(Week), week.ToString()));

            return new Period(PeriodType.Weekly, year, month, null, week);
        }

        public static Period CreateMonthly(int year, int month)
        {
            if (year < 1900 || year > 9999)
                throw new DomainException(nameof(Period), DomainErrors.FieldInvalid(nameof(Year), year.ToString()));

            if (month < 1 || month > 12)
                throw new DomainException(nameof(Period), DomainErrors.FieldInvalid(nameof(Month), month.ToString()));

            return new Period(PeriodType.Monthly, year, month, null, null);
        }

        public static Period CreateYearly(int year)
        {
            if (year < 1900 || year > 9999)
                throw new DomainException(nameof(Period), DomainErrors.FieldInvalid(nameof(Year), year.ToString()));

            return new Period(PeriodType.Yearly, year, 1, null, null);
        }

        public bool IsDaily => Type.Equals(PeriodType.Daily);
        public bool IsWeekly => Type.Equals(PeriodType.Weekly);
        public bool IsMonthly => Type.Equals(PeriodType.Monthly);
        public bool IsYearly => Type.Equals(PeriodType.Yearly);

        public DateOnly? GetDate()
        {
            if (IsDaily && Day.HasValue)
                return new DateOnly(Year, Month, Day.Value);

            return null;
        }

        private static int GetWeekOfYear(DateTime date)
        {
            var calendar = CultureInfo.CurrentCulture.Calendar;
            var weekRule = CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule;
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            return calendar.GetWeekOfYear(date, weekRule, firstDayOfWeek);
        }

        public static int GetMonthFromWeekOfYear(int year, int week)
        {
            var calendar = CultureInfo.CurrentCulture.Calendar;
            var weekRule = CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule;
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;

            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = (int)firstDayOfWeek - (int)jan1.DayOfWeek;
            var firstWeekDay = jan1.AddDays(daysOffset);

            var targetDate = firstWeekDay.AddDays((week - 1) * 7);

            if (targetDate.Year != year)
            {
                targetDate = new DateTime(year, 1, 1);
                while (calendar.GetWeekOfYear(targetDate, weekRule, firstDayOfWeek) != week)
                {
                    targetDate = targetDate.AddDays(1);
                    if (targetDate.Year > year)
                        return 1;
                }
            }

            return targetDate.Month;
        }

        public override string ToString()
        {
            if (Type.Equals(PeriodType.Daily))
                return $"Daily: {Year}-{Month:D2}-{Day:D2}";

            if (Type.Equals(PeriodType.Weekly))
                return $"Weekly: Year {Year}, Week {Week}";

            if (Type.Equals(PeriodType.Monthly))
                return $"Monthly: {Year}-{Month:D2}";

            if (Type.Equals(PeriodType.Yearly))
                return $"Yearly: {Year}";

            return base.ToString();
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Type;
            yield return Year;
            yield return Month;
            yield return Day;
            yield return Week;
        }
    }
}
