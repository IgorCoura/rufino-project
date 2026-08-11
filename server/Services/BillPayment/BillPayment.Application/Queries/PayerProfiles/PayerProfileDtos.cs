namespace BillPayment.Application.Queries.PayerProfiles;

public sealed record PayerProfileDto(
    Guid Id,
    string Kind,
    string LegalName,
    string PrimaryTaxId,
    string PrimaryTaxIdKind,
    IReadOnlyList<PayerProfileTaxIdDto> AdditionalTaxIds,
    bool MatchByCnpjRoot,
    bool CanSchedulePayments);

/// <summary>
/// A referência da subconta não entra no DTO de propósito: é ponteiro para segredo, e o
/// que a interface precisa saber é apenas se o tenant já pode agendar pagamento.
/// </summary>
public sealed record PayerProfileTaxIdDto(string Value, string Kind);
