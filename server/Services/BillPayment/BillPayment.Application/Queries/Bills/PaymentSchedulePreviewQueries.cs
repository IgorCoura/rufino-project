namespace BillPayment.Application.Queries.Bills;

using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Domain.Bills;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// O que o sheet de aprovar mostra ANTES de aprovar: a data em que a submissão realmente
/// ocorreria para a data pedida.
/// </summary>
/// <remarks>
/// Serve a data LIVRE do seletor. As quatro sugestões prontas da folha vêm do
/// <see cref="IScheduleOptionQueries"/>, que aplica quatro vezes o mesmo <see cref="For"/>.
/// </remarks>
/// <param name="RequestedDate">A data que o aprovador escolheu.</param>
/// <param name="EffectiveDate">A data que a política/calendário produziria de fato.</param>
/// <param name="Slid">A efetiva difere da pedida — piso do provedor ou dia útil.</param>
/// <param name="Immediate">O boleto está vencido: execução imediata, sem data futura (ADR-017).</param>
/// <param name="AfterDueDate">
/// A data efetiva cai depois do vencimento: o pagamento sai em atraso e pode render encargos.
/// É aviso, nunca bloqueio — pagar atrasado é justamente o que o produto precisa saber fazer.
/// Falso quando <paramref name="Immediate"/>, que já diz a mesma coisa com mais precisão.
/// </param>
public sealed record SchedulePreviewDto(
    DateOnly RequestedDate,
    DateOnly EffectiveDate,
    bool Slid,
    bool Immediate,
    bool AfterDueDate)
{
    /// <summary>
    /// A conta, sem I/O — o mesmo <see cref="PaymentSchedulingService"/> da fila, para que a
    /// prévia do seletor livre e as quatro sugestões da folha nunca divirjam da submissão.
    /// </summary>
    public static SchedulePreviewDto For(
        DateOnly requestedDate,
        DateOnly? dueDate,
        DateOnly? minimumScheduleDate,
        DateTime nowLocal,
        PaymentSchedulingPolicy policy,
        IWorkingDayCalendar calendar)
    {
        var resolution = PaymentSchedulingService.Resolve(
            requestedDate, dueDate, minimumScheduleDate, nowLocal, policy, calendar);

        // Imediato: não há data futura — a efetiva é "hoje" no fuso da política, sem deslize
        // (o que há a comunicar é a execução na hora, não uma data que mudou).
        if (resolution.RequiresImmediateExecution)
        {
            return new SchedulePreviewDto(
                requestedDate, DateOnly.FromDateTime(nowLocal),
                Slid: false, Immediate: true, AfterDueDate: false);
        }

        var effective = resolution.EffectiveDate!.Value;

        return new SchedulePreviewDto(
            requestedDate,
            effective,
            Slid: effective != requestedDate,
            Immediate: false,
            AfterDueDate: dueDate is { } due && effective > due);
    }
}

public interface IPaymentSchedulePreviewQueries
{
    /// <summary>Nulo quando o boleto não é deste tenant — o controller colapsa em 404.</summary>
    Task<SchedulePreviewDto?> PreviewAsync(
        Guid tenantId,
        Guid billId,
        DateOnly requestedDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Leitura pura: projeta os dois fatos do boleto que a política consome (vencimento e piso do
/// provedor) e roda o MESMO <see cref="PaymentSchedulingService"/> da fila — a prévia nunca
/// diverge da submissão porque é o mesmo cálculo, com o mesmo calendário e o mesmo fuso.
/// </summary>
internal sealed class PaymentSchedulePreviewQueries(
    BillPaymentDbContext context,
    IWorkingDayCalendar calendar,
    IOptions<PaymentSchedulingOptions> options,
    TimeProvider clock) : IPaymentSchedulePreviewQueries
{
    public async Task<SchedulePreviewDto?> PreviewAsync(
        Guid tenantId,
        Guid billId,
        DateOnly requestedDate,
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
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(
            clock.GetUtcNow().UtcDateTime, scheduling.ResolveTimeZone());

        return SchedulePreviewDto.For(
            requestedDate, facts.DueDate, facts.MinimumScheduleDate,
            nowLocal, scheduling.ToPolicy(), calendar);
    }
}
