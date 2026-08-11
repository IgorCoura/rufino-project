namespace BillPayment.Application.Models.Payees;

using System.Text.Json.Serialization;
using BillPayment.Application.Payees.Commands;

/// <summary>
/// Modelos HTTP existem porque o <c>tenantId</c> vem da rota, não do corpo — mandar o
/// tenant no body abriria caminho para divergir do path e virar IDOR.
/// </summary>
/// <remarks>
/// Os quatro campos de valor são todos opcionais no contrato porque cada tipo de política
/// usa um subconjunto deles. Quem cobra a combinação correta é o domínio
/// (<c>AmountPolicy.From</c>), não a desserialização — assim a regra fica em um lugar só.
/// </remarks>
public sealed record RegisterPayeeModel(
    string LegalName,
    string TaxId,
    string AmountPolicyKind,
    decimal? ExpectedAmount,
    decimal? TolerancePercent,
    decimal? MinAmount,
    decimal? MaxAmount)
{
    public RegisterPayeeCommand ToCommand(Guid tenantId)
        => new(tenantId, LegalName, TaxId, AmountPolicyKind, ExpectedAmount, TolerancePercent, MinAmount, MaxAmount);
}

public sealed record RenamePayeeModel(string LegalName)
{
    public RenamePayeeCommand ToCommand(Guid tenantId, Guid payeeId) => new(tenantId, payeeId, LegalName);
}

public sealed record AlterPayeeAmountPolicyModel(
    string AmountPolicyKind,
    decimal? ExpectedAmount,
    decimal? TolerancePercent,
    decimal? MinAmount,
    decimal? MaxAmount)
{
    public AlterPayeeAmountPolicyCommand ToCommand(Guid tenantId, Guid payeeId)
        => new(tenantId, payeeId, AmountPolicyKind, ExpectedAmount, TolerancePercent, MinAmount, MaxAmount);
}

public sealed record PayeeAliasModel(string Alias)
{
    public AddPayeeAliasCommand ToAddCommand(Guid tenantId, Guid payeeId) => new(tenantId, payeeId, Alias);
}

public sealed record PayeeBankModel(string BankCode)
{
    public AllowPayeeBankCommand ToAllowCommand(Guid tenantId, Guid payeeId) => new(tenantId, payeeId, BankCode);
}

/// <summary>
/// <c>[JsonRequired]</c> impede que a omissão de <c>IsActive</c> vire <c>false</c> em
/// silêncio e desative um beneficiário sem que ninguém tenha pedido.
/// </summary>
public sealed record AlterPayeeActivationModel([property: JsonRequired] bool IsActive)
{
    public AlterPayeeActivationCommand ToCommand(Guid tenantId, Guid payeeId) => new(tenantId, payeeId, IsActive);
}
