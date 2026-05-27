namespace EconomicCore.Application.Commands.RegisterPaymentEvent;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.Services;
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
        var currency = Enumeration.FromDisplayName<Currency>(request.Currency);
        var paymentAmount = new Money(request.Amount, currency);

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        var period = new CompetencePeriod(request.Year, request.Month);
        var outflowCommitment = contract.FindPromisedCommitment(period, CommitmentDirection.OutflowPromise);
        var inflowCommitment = contract.FindReciprocalCommitment(outflowCommitment.Id);

        var cashResourceId = EconomicResourceId.New();

        var participations = new List<Participation>
        {
            new(EconomicAgentId.From(tenantId.Value), ParticipationRole.Provider),
            new(contract.CounterpartyId, ParticipationRole.Recipient),
        };

        var paymentEvent = EconomicEvent.RegisterCovered(
            EconomicEventId.New(),
            tenantId,
            FlowDirection.Outflow,
            cashResourceId,
            paymentAmount,
            new EventTimestamp(request.OccurredAt),
            typeId: null,
            participations,
            new CommitmentRef(outflowCommitment.Id),
            period,
            createdBy: null,
            registeredAt: _timeProvider.GetUtcNow().UtcDateTime);

        await _eventRepo.InsertAsync(paymentEvent, cancellationToken);

        var consumptionEvent = await _eventRepo.FindCoveredByCommitmentAsync(
            inflowCommitment.Id, tenantId, cancellationToken);

        if (consumptionEvent is not null)
        {
            DualityMatchingService.Match(paymentEvent, consumptionEvent, request.OccurredAt);
            contract.MarkFulfilled(outflowCommitment.Id, paymentEvent.Id, request.OccurredAt);
            contract.MarkFulfilled(inflowCommitment.Id, consumptionEvent.Id, request.OccurredAt);
        }

        await _eventRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterPaymentEventResponse(
            paymentEvent.Id.Value,
            paymentEvent.Direction.Name,
            paymentEvent.Direction == FlowDirection.Inflow ? "Service" : "Cash");
    }
}
