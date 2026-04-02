namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.WorkloadCalendar
{
    public class BrazilianHolidayProvider : IHolidayProvider
    {
        private readonly Dictionary<int, HashSet<DateOnly>> _cache = [];

        public bool IsHoliday(DateOnly date)
        {
            return GetHolidays(date.Year).Contains(date);
        }

        public HashSet<DateOnly> GetHolidays(int year)
        {
            if (_cache.TryGetValue(year, out var cached))
                return cached;

            var holidays = new HashSet<DateOnly>();

            // Feriados fixos
            holidays.Add(new DateOnly(year, 1, 1));   // Confraternização Universal
            holidays.Add(new DateOnly(year, 4, 21));   // Tiradentes
            holidays.Add(new DateOnly(year, 5, 1));    // Dia do Trabalho
            holidays.Add(new DateOnly(year, 9, 7));    // Independência do Brasil
            holidays.Add(new DateOnly(year, 10, 12));  // Nossa Senhora Aparecida
            holidays.Add(new DateOnly(year, 11, 2));   // Finados
            holidays.Add(new DateOnly(year, 11, 15));  // Proclamação da República
            holidays.Add(new DateOnly(year, 12, 25));  // Natal

            // Feriados móveis baseados na Páscoa
            var easter = CalculateEaster(year);
            holidays.Add(easter.AddDays(-47)); // Carnaval (segunda)
            holidays.Add(easter.AddDays(-46)); // Carnaval (terça)
            holidays.Add(easter.AddDays(-2));  // Sexta-feira Santa
            holidays.Add(easter.AddDays(60));  // Corpus Christi

            _cache[year] = holidays;
            return holidays;
        }

        /// <summary>
        /// Algoritmo Gregoriano Anônimo para cálculo da Páscoa.
        /// </summary>
        private static DateOnly CalculateEaster(int year)
        {
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateOnly(year, month, day);
        }
    }
}
