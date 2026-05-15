namespace AccountsPayable.Domain.Services;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.InstallmentPlans;
using AccountsPayable.Domain.InstallmentPlans.Enumerations;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Stateless Domain Service that builds a coherent <see cref="InstallmentPlan"/> + chain of
/// <see cref="Payable"/>s for a parcelamento. Lives in the Service layer because it crosses
/// two Aggregates and owns cents-distribution semantics that don't belong to either one
/// alone. The Application layer persists both Aggregates atomically (single Unit of Work).
/// <para>
/// <b>Residue allocation</b>: when <c>TotalAmount / InstallmentCount</c> has a remainder,
/// the cent residue is added to the <b>last</b> installment — e.g., R$ 1000.00 in 3x produces
/// <c>[333.33, 333.33, 333.34]</c>. The first installment is left clean; the last one absorbs
/// the difference so the chain sums back exactly to <c>TotalAmount</c>.
/// </para>
/// </summary>
public sealed class InstallmentPlanFactory
{
    public InstallmentPlanFactoryResult Create(
        InstallmentPlanId planId,
        TenantId tenantId,
        SupplierId supplierId,
        Money totalAmount,
        int installmentCount,
        DateOnly firstDueDate,
        InstallmentFrequency frequency,
        Description description,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(totalAmount);
        ArgumentNullException.ThrowIfNull(frequency);
        ArgumentNullException.ThrowIfNull(description);

        var plan = InstallmentPlan.Create(
            id: planId,
            tenantId: tenantId,
            supplierId: supplierId,
            totalAmount: totalAmount,
            installmentCount: installmentCount,
            firstDueDate: firstDueDate,
            frequency: frequency,
            description: description,
            occurredAt: occurredAt);

        var amounts = DistributeCents(totalAmount.Amount, installmentCount);
        if (amounts.Sum() != totalAmount.Amount)
            throw InstallmentPlanErrors.AmountSumMismatch(totalAmount.Amount, amounts.Sum());

        var payables = new List<Payable>(installmentCount);
        for (var i = 0; i < installmentCount; i++)
        {
            var installmentNumber = i + 1;
            var dueDate = frequency.DueDateFor(firstDueDate, i);
            var installmentAmount = new Money(amounts[i], totalAmount.Currency);
            var installmentDescription = new Description(
                $"{description.Value} ({installmentNumber}/{installmentCount})");

            var payable = Payable.InitializeAsInstallment(
                id: PayableId.New(),
                tenantId: tenantId,
                installmentPlanId: planId,
                installmentNumber: installmentNumber,
                supplierId: supplierId,
                amount: installmentAmount,
                dueDate: new DueDate(dueDate),
                description: installmentDescription,
                occurredAt: occurredAt);

            plan.RegisterPayable(payable.Id, installmentNumber, occurredAt);
            payables.Add(payable);
        }

        return new InstallmentPlanFactoryResult(plan, payables.AsReadOnly());
    }

    private static decimal[] DistributeCents(decimal totalAmount, int installmentCount)
    {
        var totalCents = (long)Math.Round(totalAmount * 100m, MidpointRounding.ToEven);
        var baseCents = totalCents / installmentCount;
        var residueCents = totalCents - baseCents * installmentCount;

        var amounts = new decimal[installmentCount];
        for (var i = 0; i < installmentCount; i++)
        {
            var cents = baseCents + (i == installmentCount - 1 ? residueCents : 0);
            amounts[i] = cents / 100m;
        }
        return amounts;
    }
}

public sealed record InstallmentPlanFactoryResult(
    InstallmentPlan Plan,
    IReadOnlyList<Payable> Payables);
