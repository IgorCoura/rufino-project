namespace EconomicCore.Application.Commands.RegisterConsumptionEvent;

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

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        var period = new CompetencePeriod(request.Year, request.Month);
        var inflowCommitment = contract.FindPromisedCommitment(period, CommitmentDirection.InflowPromise);

        var participations = new List<Participation>
        {
            new(contract.CounterpartyId, ParticipationRole.Provider),
            new(EconomicAgentId.From(tenantId.Value), ParticipationRole.Recipient),
        };

        var serviceResourceId = FindServiceResource(contract);
        var eventAmount = new Money(inflowCommitment.ExpectedAmount.Amount, inflowCommitment.ExpectedAmount.Currency);

        var economicEvent = EconomicEvent.RegisterCovered(
            EconomicEventId.New(),
            tenantId,
            FlowDirection.Inflow,
            serviceResourceId,
            eventAmount,
            new EventTimestamp(request.OccurredAt),
            typeId: null,
            participations,
            new CommitmentRef(inflowCommitment.Id),
            period,
            createdBy: null,
            registeredAt: _timeProvider.GetUtcNow().UtcDateTime);

        await _eventRepo.InsertAsync(economicEvent, cancellationToken);

        var outflowCommitment = contract.FindReciprocalCommitment(inflowCommitment.Id);
        var paymentEvent = await _eventRepo.FindCoveredByCommitmentAsync(
            outflowCommitment.Id, tenantId, cancellationToken);

        if (paymentEvent is not null)
        {
            DualityMatchingService.Match(paymentEvent, economicEvent, request.OccurredAt);
            contract.MarkFulfilled(outflowCommitment.Id, paymentEvent.Id, request.OccurredAt);
            contract.MarkFulfilled(inflowCommitment.Id, economicEvent.Id, request.OccurredAt);
        }

        await _eventRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterConsumptionEventResponse(
            economicEvent.Id.Value,
            economicEvent.Direction.Name,
            economicEvent.Direction == FlowDirection.Inflow ? "Service" : "Cash");
    }

    private static EconomicResourceId FindServiceResource(EconomicContract contract)
    {
        _ = contract;
        return EconomicResourceId.New();
    }
}
