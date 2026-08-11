namespace BillPayment.IntegrationTests.Contracts;

// DTOs DUPLICADOS de propósito — ver a nota em PayeeContracts.

internal sealed record RegisterPayerProfileRequest(
    string Kind,
    string LegalName,
    string PrimaryTaxId);

internal sealed record RenamePayerProfileRequest(string LegalName);

internal sealed record PayerProfileTaxIdRequest(string TaxId);

internal sealed record AlterCnpjRootMatchingRequest(bool Enabled);

internal sealed record LinkAsaasAccountRequest(string? AccountRef);

internal sealed record PayerProfileIdResponse(Guid Id);

internal sealed record LinkAsaasAccountResponseContract(Guid Id, bool CanSchedulePayments);

internal sealed record PayerProfileTaxIdResponse(string Value, string Kind);

internal sealed record PayerProfileResponse(
    Guid Id,
    string Kind,
    string LegalName,
    string PrimaryTaxId,
    string PrimaryTaxIdKind,
    IReadOnlyList<PayerProfileTaxIdResponse> AdditionalTaxIds,
    bool MatchByCnpjRoot,
    bool CanSchedulePayments);
