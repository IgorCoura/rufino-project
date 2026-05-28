namespace EconomicCore.Application.Commands.TerminateEconomicContract;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SharedKernel;
using MediatR;

internal sealed class TerminateEconomicContractHandler : IRequestHandler<TerminateEconomicContractCommand, TerminateEconomicContractResponse>
{
    private readonly IEconomicContractRepository _contractRepo;
    private readonly IEconomicEventRepository _eventRepo;
    private readonly TimeProvider _timeProvider;

    public TerminateEconomicContractHandler(
        IEconomicContractRepository contractRepo,
        IEconomicEventRepository eventRepo,
        TimeProvider timeProvider)
    {
        _contractRepo = contractRepo;
        _eventRepo = eventRepo;
        _timeProvider = timeProvider;
    }

    public async Task<TerminateEconomicContractResponse> Handle(TerminateEconomicContractCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var contractId = EconomicContractId.From(request.ContractId);

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        var inflowCommitmentIds = contract.Commitments
            .Where(c => c.Direction == CommitmentDirection.InflowPromise)
            .Select(c => c.Id)
            .ToList();

        if (inflowCommitmentIds.Count > 0)
        {
            var lastInflowPeriod = await _eventRepo.GetLastInflowPeriodForCommitmentsAsync(
                inflowCommitmentIds, tenantId, cancellationToken);

            if (lastInflowPeriod is not null)
            {
                var lastDayOfLastOccupied = lastInflowPeriod.LastDay();
                if (request.TerminationDate < lastDayOfLastOccupied)
                {
                    throw EconomicContractErrors.InvalidTerminationDate(
                        request.TerminationDate,
                        $"{lastInflowPeriod.Year:D4}-{lastInflowPeriod.Month:D2}");
                }
            }
        }

        var occurredAt = _timeProvider.GetUtcNow().UtcDateTime;

        var commitmentsToCancel = contract.Commitments
            .Where(c => (c.Status == CommitmentStatus.Promised || c.Status == CommitmentStatus.Reserved)
                && c.Period.FirstDay() > request.TerminationDate)
            .Select(c => c.Id)
            .ToList();

        foreach (var commitmentId in commitmentsToCancel)
        {
            contract.CancelCommitment(commitmentId, occurredAt);
        }

        contract.Terminate(occurredAt);

        _contractRepo.Update(contract);
        await _contractRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new TerminateEconomicContractResponse(
            contract.Id.Value,
            contract.Status.Name,
            commitmentsToCancel.Count);
    }
}
