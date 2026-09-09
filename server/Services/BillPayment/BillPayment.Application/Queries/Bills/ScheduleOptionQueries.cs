namespace BillPayment.Application.Queries.Bills;

using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// As quatro datas que a folha de agendar oferece prontas (ADR-021).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Vive na Application, não no Domain, de propósito.</strong> "Um dia antes do
/// vencimento" é sugestão de tela, não invariante de <c>Bill</c>: nada no domínio fica
/// inconsistente se ninguém escolher essa data. O agregado continua conhecendo só datas, e o
/// contrato de escrita (<c>scheduleFor</c>) continua sendo uma data — quem quiser outro dia
/// escolhe no seletor e passa pela mesma porta.
/// </para>
/// <para>
/// A conta é do SERVIDOR e não se repete na tela. Ela depende de "hoje" no fuso da política, do
/// calendário bancário e do piso do provedor — três coisas que o cliente não tem, e a última
/// tentativa de derivar dia no cliente é o que o cinto do <c>BLP.BIL35</c> existe para remendar.
/// </para>
/// </remarks>
public static class ScheduleOptions
{
    /// <summary>Hoje. A única sugestão que depende da HORA.</summary>
    public const string TODAY = "Today";

    /// <summary>Amanhã.</summary>
    public const string TOMORROW = "Tomorrow";

    /// <summary>Véspera do vencimento.</summary>
    public const string DAY_BEFORE_DUE = "DayBeforeDue";

    /// <summary>O próprio dia do vencimento.</summary>
    public const string ON_DUE_DATE = "OnDueDate";

    /// <summary>O boleto não tem vencimento conhecido — não há de onde tirar a data.</summary>
    public const string REASON_NO_DUE_DATE = "no_due_date";

    /// <summary>A data que a sugestão produz já passou.</summary>
    public const string REASON_IN_THE_PAST = "in_the_past";
}

/// <param name="Option">Um dos nomes de <see cref="ScheduleOptions"/>.</param>
/// <param name="Date">A data que esta sugestão manda em <c>scheduleFor</c>; nula se indisponível.</param>
/// <param name="Preview">A prévia dessa data — mesma forma do seletor livre; nula se indisponível.</param>
/// <param name="Available">A sugestão pode ser escolhida agora.</param>
/// <param name="UnavailableReason">
/// Código do motivo quando não pode. É código, e não frase, porque quem escreve o texto é a
/// tela — o mesmo motivo aparece em contextos com palavras diferentes.
/// </param>
public sealed record ScheduleOptionDto(
    string Option,
    DateOnly? Date,
    SchedulePreviewDto? Preview,
    bool Available,
    string? UnavailableReason);

public interface IScheduleOptionQueries
{
    /// <summary>Nulo quando o boleto não é deste tenant — o controller colapsa em 404.</summary>
    Task<IReadOnlyList<ScheduleOptionDto>?> ListAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default);
}

internal sealed class ScheduleOptionQueries(
    BillPaymentDbContext context,
    IWorkingDayCalendar calendar,
    IOptions<PaymentSchedulingOptions> options,
    TimeProvider clock) : IScheduleOptionQueries
{
    public async Task<IReadOnlyList<ScheduleOptionDto>?> ListAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = BillId.From(billId);

        var facts = await context.Bills
            .AsNoTracking()
            .Where(b => b.TenantId == tenant && b.Id == id)
            .Select(b => new { b.DueDate, b.Lookup!.MinimumScheduleDate })
            .FirstOrDefaultAsync(cancellationToken);

        if (facts is null)
            return null;

        var scheduling = options.Value;
        var policy = scheduling.ToPolicy();
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(
            clock.GetUtcNow().UtcDateTime, scheduling.ResolveTimeZone());
        var today = DateOnly.FromDateTime(nowLocal);

        // O MESMO veredito que o agregado usa para recusar (BLP.BIL40): a folha oferece
        // exatamente o que o servidor aceitaria, e nunca leva alguém a um erro já conhecido.
        var sameDay = PaymentSchedulingService.CanScheduleForToday(
            facts.DueDate, facts.MinimumScheduleDate, nowLocal, policy, calendar);

        var results = new List<ScheduleOptionDto>(4)
        {
            Build(ScheduleOptions.TODAY, today, sameDay.Allowed, sameDay.ReasonCode),
            Build(ScheduleOptions.TOMORROW, today.AddDays(1), available: true, reason: null),
        };

        results.Add(FromDueDate(ScheduleOptions.DAY_BEFORE_DUE, facts.DueDate?.AddDays(-1), today));
        results.Add(FromDueDate(ScheduleOptions.ON_DUE_DATE, facts.DueDate, today));

        return results;

        ScheduleOptionDto Build(string option, DateOnly date, bool available, string? reason)
            => available
                ? new ScheduleOptionDto(
                    option,
                    date,
                    SchedulePreviewDto.For(
                        date, facts.DueDate, facts.MinimumScheduleDate, nowLocal, policy, calendar),
                    Available: true,
                    UnavailableReason: null)
                : new ScheduleOptionDto(option, Date: null, Preview: null, Available: false, reason);

        ScheduleOptionDto FromDueDate(string option, DateOnly? date, DateOnly floor) => date switch
        {
            null => Build(option, floor, available: false, ScheduleOptions.REASON_NO_DUE_DATE),

            // Véspera de um vencimento que é hoje cai em ontem; vencimento passado cai em
            // passado. Nos dois casos a sugestão não existe — e "pagar hoje" logo acima já é a
            // resposta certa para quem está em cima da hora.
            { } d when d < floor => Build(option, floor, available: false, ScheduleOptions.REASON_IN_THE_PAST),

            // A véspera que cai em HOJE é a mesma escolha do "pagar hoje", e herda a hora dele.
            { } d when d == floor => Build(option, d, sameDay.Allowed, sameDay.ReasonCode),

            { } d => Build(option, d, available: true, reason: null),
        };
    }
}
