namespace EconomicCore.Application.Commands.ActivateEconomicContract;

using EconomicCore.Application.Commands.GenerateCommitments;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SharedKernel;
using MediatR;

internal sealed class ActivateEconomicContractHandler : IRequestHandler<ActivateEconomicContractCommand, ActivateEconomicContractResponse>
{
    private readonly IEconomicContractRepository _contractRepo;
    private readonly TimeProvider _timeProvider;

    public ActivateEconomicContractHandler(
        IEconomicContractRepository contractRepo,
        TimeProvider timeProvider)
    {
        _contractRepo = contractRepo;
        _timeProvider = timeProvider;
    }

    public async Task<ActivateEconomicContractResponse> Handle(ActivateEconomicContractCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var contractId = EconomicContractId.From(request.ContractId);

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        contract.Activate(_timeProvider.GetUtcNow().UtcDateTime, CommitmentId.New);

        _contractRepo.Update(contract);
        await _contractRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        var commitments = contract.Commitments
            .OrderBy(c => c.Period.Year).ThenBy(c => c.Period.Month).ThenBy(c => c.Direction.Id)
            .Select(c => new CommitmentDto(c.Id.Value, c.Direction.Name, c.Status.Name, c.Period.Year, c.Period.Month))
            .ToList();

        return new ActivateEconomicContractResponse(
            contract.Id.Value,
            contract.Status.Name,
            commitments);
    }
}
