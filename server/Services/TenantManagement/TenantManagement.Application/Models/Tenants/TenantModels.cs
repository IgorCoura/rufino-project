namespace TenantManagement.Application.Models.Tenants;

using TenantManagement.Application.Tenants.Commands;

/// <summary>
/// Modelos HTTP. Existem para separar o corpo da requisição do Command — o id do tenant vem
/// da rota nas operações de alteração, e aceitá-lo também no corpo abriria caminho para os
/// dois divergirem.
/// </summary>
public sealed record RegisterTenantModel(
    string Kind,
    string LegalName,
    string? TradeName,
    string PrimaryTaxId,
    string ContactEmail,
    string? ContactPhone,
    AddressInput Address,
    string OwnerEmail,
    IReadOnlyList<string>? Products,
    Guid? Id)
{
    public RegisterTenantCommand ToCommand()
        => new(Kind, LegalName, TradeName, PrimaryTaxId, ContactEmail, ContactPhone, Address, OwnerEmail, Products, Id);
}

public sealed record EditTenantDetailsModel(string LegalName, string? TradeName)
{
    public EditTenantDetailsCommand ToCommand(Guid tenantId) => new(tenantId, LegalName, TradeName);
}

public sealed record ChangeTenantAddressModel(AddressInput Address)
{
    public ChangeTenantAddressCommand ToCommand(Guid tenantId) => new(tenantId, Address);
}

public sealed record ChangeTenantContactModel(string Email, string? Phone)
{
    public ChangeTenantContactCommand ToCommand(Guid tenantId) => new(tenantId, Email, Phone);
}

public sealed record SuspendTenantModel(string Reason)
{
    public SuspendTenantCommand ToCommand(Guid tenantId) => new(tenantId, Reason);
}

public sealed record GrantTenantMembershipModel(string Email, string Role)
{
    public GrantTenantMembershipCommand ToCommand(Guid tenantId) => new(tenantId, Email, Role);
}
