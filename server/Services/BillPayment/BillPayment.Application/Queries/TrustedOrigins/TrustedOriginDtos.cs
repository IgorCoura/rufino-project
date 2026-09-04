namespace BillPayment.Application.Queries.TrustedOrigins;

public sealed record TrustedOriginDto(
    Guid Id,
    string Kind,
    string Value,
    string Decision,
    Guid DecidedBy,
    DateTime DecidedAt,
    string? Note);

public sealed record TrustedOriginPage(IReadOnlyList<TrustedOriginDto> Items, string? NextCursor);
