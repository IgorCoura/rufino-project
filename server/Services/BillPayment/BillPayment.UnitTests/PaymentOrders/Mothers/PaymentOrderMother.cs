namespace BillPayment.UnitTests.PaymentOrders.Mothers;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SharedKernel;

internal static class PaymentOrderMother
{
    public static readonly DateTime DefaultOccurredAt = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DefaultTenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    public static readonly BillId DefaultBill = BillId.From(new Guid("0195a1f0-0000-7000-8000-0000000000b1"));
    public static readonly DateOnly DefaultScheduleFor = new(2026, 9, 10);

    public static Money Brl(decimal amount) => new(amount, Currency.BRL);

    /// <summary>Caminho feliz: ordem recém-nascida da aprovação, elegível para a fila.</summary>
    public static PaymentOrder Draft(
        PaymentRail? rail = null,
        Money? amount = null,
        DateOnly? requestedScheduleDate = null,
        DateTime? occurredAt = null)
        => PaymentOrder.Draft(
            DefaultTenant,
            DefaultBill,
            rail ?? PaymentRail.Boleto,
            requestedScheduleDate ?? DefaultScheduleFor,
            amount ?? Brl(150.00m),
            occurredAt ?? DefaultOccurredAt);

    /// <summary>Ordem sem valor de nenhuma fonte — o cenário da guarda PMO10 da submissão.</summary>
    public static PaymentOrder DraftWithoutAmount()
        => PaymentOrder.Draft(
            DefaultTenant,
            DefaultBill,
            PaymentRail.Boleto,
            DefaultScheduleFor,
            amount: null,
            DefaultOccurredAt);

    /// <summary>Ordem já aceita pelo provedor — o estado de quem espera webhook.</summary>
    public static PaymentOrder Submitted(
        string providerOrderId = "pay_000000000001",
        DateOnly? effectiveDate = null)
    {
        var order = Draft();
        order.MarkSubmitted(providerOrderId, effectiveDate ?? DefaultScheduleFor, null, null, DefaultOccurredAt);
        order.PullDomainEvents();
        return order;
    }
}
