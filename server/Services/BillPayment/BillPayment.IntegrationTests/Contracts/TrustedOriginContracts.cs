namespace BillPayment.IntegrationTests.Contracts;

// DTOs DUPLICADOS de propósito — não reusar os da aplicação. Se um campo do contrato
// for renomeado lá, o teste tem que quebrar; reusando o mesmo tipo ele seguiria verde
// e a quebra chegaria no cliente da API.

internal sealed record RegisterTrustedOriginRequest(
    string Kind,
    string Value,
    string Decision,
    string? Note);

internal sealed record ChangeTrustedOriginDecisionRequest(
    string Decision,
    string? Note);

internal sealed record TrustedOriginIdResponse(Guid Id);

internal sealed record TrustedOriginResponse(
    Guid Id,
    string Kind,
    string Value,
    string Decision,
    Guid DecidedBy,
    DateTime DecidedAt,
    string? Note);

internal sealed record TrustedOriginPageResponse(
    IReadOnlyList<TrustedOriginResponse> Items,
    string? NextCursor);

internal sealed record ApiErrorResponse(string Id, string Message);
