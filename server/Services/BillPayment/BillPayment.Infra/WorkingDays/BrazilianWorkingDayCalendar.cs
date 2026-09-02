namespace BillPayment.Infra.WorkingDays;

using System.Collections.Concurrent;
using BillPayment.Domain.Ports;

/// <summary>
/// Feriados bancários nacionais do Brasil, <strong>calculados</strong> — não consultados.
/// </summary>
/// <remarks>
/// <para>
/// A doutrina é a do <c>BacenBankDirectory</c>: esta tabela decide a data em que dinheiro sai,
/// e buscá-la ao vivo transformaria indisponibilidade de terceiro em pagamento parado. Aqui nem
/// snapshot é preciso: o calendário bancário nacional é <em>determinístico</em> — feriados
/// fixos por lei federal mais os móveis derivados da Páscoa (algoritmo de Meeus/Butcher), que é
/// exatamente como a ANBIMA gera a planilha oficial. Mudança de lei (como a Consciência Negra
/// nacional em 2024) é mudança aqui, visível no diff.
/// </para>
/// <para>
/// Feriado <strong>municipal e estadual fica de fora de propósito</strong>, como no provedor:
/// o pague-contas processa pelo calendário nacional, e adivinhar a praça do beneficiário
/// erraria mais do que acerta. O provedor ainda empurra dia não útil para o útil seguinte do
/// lado dele — este calendário existe para a data que mostramos ser a que executa.
/// </para>
/// </remarks>
internal sealed class BrazilianWorkingDayCalendar : IWorkingDayCalendar
{
    private readonly ConcurrentDictionary<int, HashSet<DateOnly>> _holidaysByYear = new();

    public bool IsWorkingDay(DateOnly date)
        => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
            && !HolidaysOf(date.Year).Contains(date);

    public DateOnly NextWorkingDayOnOrAfter(DateOnly date)
    {
        var candidate = date;
        while (!IsWorkingDay(candidate))
            candidate = candidate.AddDays(1);

        return candidate;
    }

    private HashSet<DateOnly> HolidaysOf(int year)
        => _holidaysByYear.GetOrAdd(year, ComputeHolidays);

    private static HashSet<DateOnly> ComputeHolidays(int year)
    {
        var easter = EasterSunday(year);

        var holidays = new HashSet<DateOnly>
        {
            new(year, 1, 1),    // Confraternização Universal
            easter.AddDays(-48), // Segunda de Carnaval
            easter.AddDays(-47), // Terça de Carnaval
            easter.AddDays(-2),  // Sexta-feira Santa
            new(year, 4, 21),   // Tiradentes
            new(year, 5, 1),    // Dia do Trabalho
            easter.AddDays(60),  // Corpus Christi
            new(year, 9, 7),    // Independência
            new(year, 10, 12),  // Nossa Senhora Aparecida
            new(year, 11, 2),   // Finados
            new(year, 11, 15),  // Proclamação da República
            new(year, 12, 25),  // Natal
        };

        // Consciência Negra é feriado nacional desde a Lei 14.759/2023 — vale a partir de 2024.
        if (year >= 2024)
            holidays.Add(new DateOnly(year, 11, 20));

        return holidays;
    }

    /// <summary>Meeus/Butcher — o mesmo algoritmo por trás do calendário oficial da ANBIMA.</summary>
    private static DateOnly EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var month = (h + l - (7 * m) + 114) / 31;
        var day = ((h + l - (7 * m) + 114) % 31) + 1;

        return new DateOnly(year, month, day);
    }
}
