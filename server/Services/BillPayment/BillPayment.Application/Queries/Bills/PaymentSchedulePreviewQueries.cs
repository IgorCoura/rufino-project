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
/// O que o sheet de aprovar mostra ANTES de aprovar: a data em que a submissão realmente
/// ocorreria para a data pedida.
/// </summary>
/// <param name="RequestedDate">A data que o aprovador escolheu.</param>
/// <param name="EffectiveDate">A data que a política/calendário produziria de fato.</param>
/// <param name="Slid">A efetiva difere da pedida — piso do provedor, antecedência ou dia útil.</param>
/// <param name="Immediate">O boleto está vencido: execução imediata, sem data futura (ADR-017).</param>
public sealed record SchedulePreviewDto(
    DateOnly RequestedDate,
    DateOnly EffectiveDate,
    bool Slid,
    bool Immediate);

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

        var resolution = PaymentSchedulingService.Resolve(
            requestedDate,
            facts.DueDate,
            facts.MinimumScheduleDate,
            nowLocal,
            scheduling.ToPolicy(),
            calendar);

        // Imediato: não há data futura — a efetiva é "hoje" no fuso da política, sem deslize
        // (o que há a comunicar é a execução na hora, não uma data que mudou).
        return resolution.RequiresImmediateExecution
            ? new SchedulePreviewDto(requestedDate, DateOnly.FromDateTime(nowLocal), Slid: false, Immediate: true)
            : new SchedulePreviewDto(
                requestedDate,
                resolution.EffectiveDate!.Value,
                Slid: resolution.EffectiveDate!.Value != requestedDate,
                Immediate: false);
    }
}
