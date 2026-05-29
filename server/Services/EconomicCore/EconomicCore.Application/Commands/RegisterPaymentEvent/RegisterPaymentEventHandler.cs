namespace EconomicCore.Application.Commands.RegisterPaymentEvent;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using MediatR;

internal sealed class RegisterPaymentEventHandler : IRequestHandler<RegisterPaymentEventCommand, RegisterPaymentEventResponse>
{
    private readonly IEconomicContractRepository _contractRepo;
    private readonly IEconomicEventRepository _eventRepo;
    private readonly TimeProvider _timeProvider;

    public RegisterPaymentEventHandler(
        IEconomicContractRepository contractRepo,
        IEconomicEventRepository eventRepo,
        TimeProvider timeProvider)
    {
        _contractRepo = contractRepo;
        _eventRepo = eventRepo;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterPaymentEventResponse> Handle(RegisterPaymentEventCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var contractId = EconomicContractId.From(request.ContractId);
        var commitmentId = CommitmentId.From(request.CommitmentId);
        var currency = Enumeration.FromDisplayName<Currency>(request.Currency);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        // Pre-condições de cobertura (Active, direction, status, valor exato) vivem no agregado dono do commitment.
        contract.EnsurePayable(commitmentId, request.Amount);
        var commitment = contract.FindCommitment(commitmentId);

        // Anti-duplicata: orquestração de I/O — protege mesmo antes do par chegar para fechar a duality.
        var existingCoverage = await _eventRepo.FindCoveredByCommitmentAsync(commitment.Id, tenantId, cancellationToken);
        if (existingCoverage is not null)
            throw EconomicContractErrors.CannotFulfillInStatus(commitment.Status.Name);

        var createdBy = request.UserId is { } uid ? UserId.From(uid) : (UserId?)null;
        var cashResourceId = EconomicResourceId.New();

        var paymentEvent = EconomicEvent.RegisterPaymentCoverage(
            EconomicEventId.New(),
            tenantId,
            cashResourceId,
            request.Amount,
            currency,
            request.OccurredAt,
            payerAgentId: EconomicAgentId.From(tenantId.Value),
            payeeAgentId: contract.CounterpartyId,
            coveringCommitmentId: commitment.Id,
            competenceYear: commitment.Period.Year,
            competenceMonth: commitment.Period.Month,
            createdBy: createdBy,
            registeredAt: now);

        await _eventRepo.InsertAsync(paymentEvent, cancellationToken);
        await _eventRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // A duality e o MarkFulfilled acontecem de forma assíncrona, reagindo ao EconomicEventRegistered
        // via Outbox (CloseDualityOnEconomicEventRegisteredHandler) — o comando muta um único agregado.
        return new RegisterPaymentEventResponse(
            paymentEvent.Id.Value,
            paymentEvent.Direction.Name,
            "Cash");
    }
}
