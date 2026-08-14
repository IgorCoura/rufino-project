namespace TenantManagement.IntegrationTests.Contracts;

/// <summary>
/// Cópias dos contratos HTTP, de propósito. Reusar os modelos da aplicação faria uma
/// renomeação de propriedade passar despercebida — o teste continuaria verde e o cliente da
/// API quebraria. Duplicados, é o teste que avisa.
/// </summary>
public sealed record AddressRequest(
    string ZipCode,
    string Street,
    string Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State,
    string? Country);

public sealed record RegisterTenantRequest(
    string Kind,
    string LegalName,
    string? TradeName,
    string PrimaryTaxId,
    string ContactEmail,
    string? ContactPhone,
    AddressRequest Address,
    string OwnerEmail,
    IReadOnlyList<string>? Products,
    Guid? Id);

public sealed record EditTenantDetailsRequest(string LegalName, string? TradeName);

public sealed record ChangeAddressRequest(AddressRequest Address);

public sealed record ChangeContactRequest(string Email, string? Phone);

public sealed record SuspendTenantRequest(string Reason);

public sealed record GrantMembershipRequest(string Email, string Role);

public sealed record IdResponse(Guid Id);

public sealed record StatusResponse(Guid Id, string Status);

public sealed record ProductResponse(Guid Id, string Product, bool IsActive);

public sealed record MembershipResponse(Guid Id, string Email, string Provisioning);

public sealed record ReprovisionResponse(Guid Id, int RequeuedMemberships, string Provisioning);

public sealed record ErrorResponse(string Id, string Message);

public sealed record TenantAddressResponse(
    string ZipCode,
    string Street,
    string Number,
    string Complement,
    string Neighborhood,
    string City,
    string State,
    string Country);

public sealed record TenantContactResponse(string Email, string Phone);

public sealed record TenantMembershipResponse(
    string Email,
    string Role,
    bool IsActive,
    string Provisioning,
    Guid? UserId);

public sealed record TenantProductResponse(string Product, bool IsActive, DateTime ActivatedAt, DateTime? DeactivatedAt);

public sealed record TenantResponse(
    Guid Id,
    string Kind,
    string LegalName,
    string TradeName,
    string PrimaryTaxId,
    string PrimaryTaxIdKind,
    string Status,
    string SuspensionReason,
    string AccessProvisioning,
    TenantContactResponse Contact,
    TenantAddressResponse Address,
    IReadOnlyList<TenantProductResponse> Products,
    IReadOnlyList<TenantMembershipResponse> Memberships);

public sealed record TenantListItemResponse(
    Guid Id,
    string Kind,
    string LegalName,
    string TradeName,
    string PrimaryTaxId,
    string Status,
    string AccessProvisioning,
    string ContactEmail,
    IReadOnlyList<string> ActiveProducts);

public sealed record TenantPageResponse(IReadOnlyList<TenantListItemResponse> Items, string? NextCursor);

public sealed record MyTenantResponse(
    Guid Id,
    string Kind,
    string LegalName,
    string TradeName,
    string Status,
    string Role,
    IReadOnlyList<string> ActiveProducts);
