namespace BillPayment.Application.Queries.Payees;

public sealed record PayeeDto(
    Guid Id,
    string LegalName,
    string TaxId,
    string TaxIdKind,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> AcceptedBanks,
    PayeeAmountPolicyDto AmountPolicy,
    bool IsActive,
    string Standing);

public sealed record PayeeAmountPolicyDto(
    string Kind,
    decimal? ExpectedAmount,
    decimal? TolerancePercent,
    decimal? MinAmount,
    decimal? MaxAmount,
    bool IsConclusive);

public sealed record PayeePage(IReadOnlyList<PayeeDto> Items, string? NextCursor);
