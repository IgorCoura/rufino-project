namespace AccountsPayable.Domain.Contracts.Events;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

public sealed record ContractCreated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ContractId ContractId,
    SupplierId SupplierId,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal MonthlyAmountValue,
    string MonthlyAmountCurrency,
    int PaymentDay,
    bool AutoCreatePayable,
    string Description) : IDomainEvent;
