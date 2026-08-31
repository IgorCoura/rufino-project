namespace BillPayment.IntegrationTests.Contracts;

// DTOs DUPLICADOS de propósito — não reusar os da aplicação. Se um campo do contrato
// for renomeado lá, o teste tem que quebrar; reusando o mesmo tipo ele seguiria verde
// e a quebra chegaria no cliente da API.

internal sealed record RegisterPayeeRequest(
    string LegalName,
    string TaxId,
    string AmountPolicyKind,
    decimal? ExpectedAmount,
    decimal? TolerancePercent,
    decimal? MinAmount,
    decimal? MaxAmount);

internal sealed record RenamePayeeRequest(string LegalName);

internal sealed record AlterPayeeAmountPolicyRequest(
    string AmountPolicyKind,
    decimal? ExpectedAmount,
    decimal? TolerancePercent,
    decimal? MinAmount,
    decimal? MaxAmount);

internal sealed record PayeeAliasRequest(string Alias);

internal sealed record PayeeBankRequest(string BankCode);

internal sealed record AlterPayeeActivationRequest(bool IsActive);

internal sealed record AlterPayeeStandingRequest(string Standing);

internal sealed record PayeeIdResponse(Guid Id);

internal sealed record PayeeAmountPolicyResponse(
    string Kind,
    decimal? ExpectedAmount,
    decimal? TolerancePercent,
    decimal? MinAmount,
    decimal? MaxAmount,
    bool IsConclusive);

internal sealed record PayeeResponse(
    Guid Id,
    string LegalName,
    string TaxId,
    string TaxIdKind,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> AcceptedBanks,
    PayeeAmountPolicyResponse AmountPolicy,
    bool IsActive,
    string Standing);

internal sealed record PayeePageResponse(
    IReadOnlyList<PayeeResponse> Items,
    string? NextCursor);
