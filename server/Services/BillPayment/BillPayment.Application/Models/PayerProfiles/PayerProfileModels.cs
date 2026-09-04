namespace BillPayment.Application.Models.PayerProfiles;

using System.Text.Json.Serialization;
using BillPayment.Application.PayerProfiles.Commands;

/// <summary>
/// Modelos HTTP existem porque o <c>tenantId</c> vem da rota, não do corpo — mandar o
/// tenant no body abriria caminho para divergir do path e virar IDOR.
/// </summary>
public sealed record RegisterPayerProfileModel(
    string Kind,
    string LegalName,
    string PrimaryTaxId)
{
    public RegisterPayerProfileCommand ToCommand(Guid tenantId)
        => new(tenantId, Kind, LegalName, PrimaryTaxId);
}

public sealed record RenamePayerProfileModel(string LegalName)
{
    public RenamePayerProfileCommand ToCommand(Guid tenantId) => new(tenantId, LegalName);
}

public sealed record PayerProfileTaxIdModel(string TaxId)
{
    public AddPayerProfileTaxIdCommand ToAddCommand(Guid tenantId) => new(tenantId, TaxId);
}

/// <summary>
/// <c>[JsonRequired]</c> impede que a omissão de <c>Enabled</c> vire <c>false</c> em silêncio
/// e desligue o casamento por raiz de CNPJ sem que ninguém tenha pedido.
/// </summary>
public sealed record AlterCnpjRootMatchingModel([property: JsonRequired] bool Enabled)
{
    public AlterCnpjRootMatchingCommand ToCommand(Guid tenantId) => new(tenantId, Enabled);
}

/// <summary>
/// <c>ApiKey</c> é a chave da subconta EM CLARO — ela é provada contra o provedor e guardada
/// cifrada no cofre; o que fica no cadastro é só o ponteiro, e a chave nunca volta pela API
/// nem aparece em log (o Command é <c>ISensitiveCommand</c>). Desvincular é o DELETE.
/// </summary>
public sealed record LinkAsaasAccountModel(string? ApiKey)
{
    public LinkAsaasAccountCommand ToCommand(Guid tenantId) => new(tenantId, ApiKey);
}
