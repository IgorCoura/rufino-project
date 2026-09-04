namespace TenantManagement.Application.Queries.Tenants;

public sealed record TenantDto(
    Guid Id,
    string Kind,
    string LegalName,
    string TradeName,
    string PrimaryTaxId,
    string PrimaryTaxIdKind,
    string Status,
    string SuspensionReason,
    string AccessProvisioning,
    TenantContactDto Contact,
    TenantAddressDto Address,
    IReadOnlyList<TenantProductDto> Products,
    IReadOnlyList<TenantMembershipDto> Memberships,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TenantContactDto(string Email, string Phone);

public sealed record TenantAddressDto(
    string ZipCode,
    string Street,
    string Number,
    string Complement,
    string Neighborhood,
    string City,
    string State,
    string Country);

public sealed record TenantProductDto(string Product, bool IsActive, DateTime ActivatedAt, DateTime? DeactivatedAt);

/// <summary>
/// O identificador da pessoa no provedor de identidade sai no DTO porque o back-office
/// precisa dele para investigar um acesso; o que nunca sai daqui é qualquer segredo.
/// </summary>
public sealed record TenantMembershipDto(
    string Email,
    string Role,
    bool IsActive,
    string Provisioning,
    Guid? UserId);

/// <summary>Linha da listagem: o suficiente para achar o tenant, sem carregar o cadastro inteiro.</summary>
public sealed record TenantListItemDto(
    Guid Id,
    string Kind,
    string LegalName,
    string TradeName,
    string PrimaryTaxId,
    string Status,
    string AccessProvisioning,
    string ContactEmail,
    IReadOnlyList<string> ActiveProducts,
    DateTime CreatedAt);

public sealed record TenantPage(IReadOnlyList<TenantListItemDto> Items, string? NextCursor);

/// <summary>O que o seletor de contexto do cliente precisa: nome, tipo e produtos habilitados.</summary>
public sealed record MyTenantDto(
    Guid Id,
    string Kind,
    string LegalName,
    string TradeName,
    string Status,
    string Role,
    IReadOnlyList<string> ActiveProducts);

/// <summary>
/// Filtros da listagem. Todos opcionais e combináveis; <c>Search</c> casa por nome, fantasia
/// ou documento, que é como um atendente procura um cliente pelo que ele disser ao telefone.
/// </summary>
public sealed record TenantListFilter(
    string? Kind = null,
    string? Status = null,
    string? Product = null,
    string? Search = null);
