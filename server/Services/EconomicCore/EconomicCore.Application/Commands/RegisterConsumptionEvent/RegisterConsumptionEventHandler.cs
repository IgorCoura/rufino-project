namespace EconomicCore.Application.Commands.RegisterConsumptionEvent;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SharedKernel;
using MediatR;

internal sealed class RegisterConsumptionEventHandler : IRequestHandler<RegisterConsumptionEventCommand, RegisterConsumptionEventResponse>
{
    private readonly IEconomicContractRepository _contractRepo;
    private readonly IEconomicEventRepository _eventRepo;
    private readonly TimeProvider _timeProvider;

    public RegisterConsumptionEventHandler(
        IEconomicContractRepository contractRepo,
        IEconomicEventRepository eventRepo,
        TimeProvider timeProvider)
    {
        _contractRepo = contractRepo;
        _eventRepo = eventRepo;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterConsumptionEventResponse> Handle(RegisterConsumptionEventCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var contractId = EconomicContractId.From(request.ContractId);
        var commitmentId = CommitmentId.From(request.CommitmentId);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        // Pre-condições de cobertura (Active, direction, status, ocupação não-futura) vivem no agregado dono do commitment.
        contract.EnsureOccupiable(commitmentId, today);
        var commitment = contract.FindCommitment(commitmentId);

        // Anti-duplicata: orquestração de I/O — protege mesmo antes do par chegar para fechar a duality.
        var existingCoverage = await _eventRepo.FindCoveredByCommitmentAsync(commitment.Id, tenantId, cancellationToken);
        if (existingCoverage is not null)
            throw EconomicContractErrors.CannotFulfillInStatus(commitment.Status.Name);

        var createdBy = request.UserId is { } uid ? UserId.From(uid) : (UserId?)null;

        var consumptionEvent = EconomicEvent.RegisterConsumptionCoverage(
            EconomicEventId.New(),
            tenantId,
            contract.ResourceId,
            commitment.ExpectedAmount.Amount,
            commitment.ExpectedAmount.Currency,
            request.OccurredAt,
            providerAgentId: contract.CounterpartyId,
            recipientAgentId: EconomicAgentId.From(tenantId.Value),
            coveringCommitmentId: commitment.Id,
            competenceYear: commitment.Period.Year,
            competenceMonth: commitment.Period.Month,
            createdBy: createdBy,
            registeredAt: now);

        await _eventRepo.InsertAsync(consumptionEvent, cancellationToken);
        await _eventRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // A duality e o MarkFulfilled acontecem de forma assíncrona, reagindo ao EconomicEventRegistered
        // via Outbox (CloseDualityOnEconomicEventRegisteredHandler) — o comando muta um único agregado.
        return new RegisterConsumptionEventResponse(
            consumptionEvent.Id.Value,
            consumptionEvent.Direction.Name,
            "Service");
    }
}
