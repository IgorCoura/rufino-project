namespace EconomicCore.Application.Commands.ApplyContractAdjustment;

public sealed record ApplyContractAdjustmentResponse(
    Guid ContractId,
    string Purpose,
    decimal NewAmount);
