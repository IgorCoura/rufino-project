namespace EconomicCore.Application.Commands.TerminateEconomicContract;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.Events;
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

        // I/O de orquestração: descobre o último período inflow ocupado (com evento registrado)
        // para alimentar a regra de data de término, que vive dentro de Terminate.
        var inflowCommitmentIds = contract.Commitments
            .Where(c => c.Direction == CommitmentDirection.InflowPromise)
            .Select(c => c.Id)
            .ToList();

        var lastOccupiedInflowPeriod = inflowCommitmentIds.Count > 0
            ? await _eventRepo.GetLastInflowPeriodForCommitmentsAsync(inflowCommitmentIds, tenantId, cancellationToken)
            : null;

        contract.Terminate(request.TerminationDate, lastOccupiedInflowPeriod, _timeProvider.GetUtcNow().UtcDateTime);

        // Reporta quantos commitments a cascata interna cancelou, lendo os eventos que o agregado emitiu.
        var cancelledCount = contract.DomainEvents.OfType<CommitmentCancelled>().Count();
        await _contractRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new TerminateEconomicContractResponse(
            contract.Id.Value,
            contract.Status.Name,
            cancelledCount);
    }
}
