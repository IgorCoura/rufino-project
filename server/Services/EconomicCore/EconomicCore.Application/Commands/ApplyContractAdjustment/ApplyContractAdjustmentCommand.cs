namespace EconomicCore.Application.Commands.ApplyContractAdjustment;

using EconomicCore.Application.Mediator;

/// <summary>
/// Mid-term adjustment (reajuste) of a contract's charge track: re-prices the still-open commitments of
/// <see cref="Purpose"/> from a competence onward. Exactly one of <see cref="NewAmount"/> (absolute) or
/// <see cref="IndexRate"/> (applied to the current amount) must be provided.
/// </summary>
public sealed record ApplyContractAdjustmentCommand(
    Guid TenantId,
    Guid ContractId,
    string Purpose,
    int EffectiveFromYear,
    int EffectiveFromMonth,
    decimal? NewAmount,
    decimal? IndexRate,
    string Currency) : IRequest<ApplyContractAdjustmentResponse>;

public sealed record ApplyContractAdjustmentModel(
    string Purpose,
    int EffectiveFromYear,
    int EffectiveFromMonth,
    decimal? NewAmount,
    decimal? IndexRate,
    string Currency)
{
    public ApplyContractAdjustmentCommand ToCommand(Guid tenantId, Guid contractId) => new(
        tenantId, contractId, Purpose, EffectiveFromYear, EffectiveFromMonth, NewAmount, IndexRate, Currency);
}
