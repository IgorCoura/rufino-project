namespace EconomicCore.Application.Commands.RegisterEconomicContract;

using EconomicCore.Application.Mediator;
using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

internal sealed class RegisterEconomicContractHandler : IRequestHandler<RegisterEconomicContractCommand, RegisterEconomicContractResponse>
{
    private readonly IEconomicContractRepository _contractRepo;
    private readonly IEconomicResourceRepository _resourceRepo;
    private readonly IEconomicAgentRepository _agentRepo;
    private readonly TimeProvider _timeProvider;

    public RegisterEconomicContractHandler(
        IEconomicContractRepository contractRepo,
        IEconomicResourceRepository resourceRepo,
        IEconomicAgentRepository agentRepo,
        TimeProvider timeProvider)
    {
        _contractRepo = contractRepo;
        _resourceRepo = resourceRepo;
        _agentRepo = agentRepo;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterEconomicContractResponse> Handle(RegisterEconomicContractCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var resourceId = EconomicResourceId.From(request.ResourceId);
        var counterpartyId = EconomicAgentId.From(request.CounterpartyId);

        if (!await _resourceRepo.ExistsAsync(resourceId, tenantId, cancellationToken))
            throw EconomicResourceErrors.ResourceNotFound(request.ResourceId);

        if (!await _agentRepo.ExistsAsync(counterpartyId, tenantId, cancellationToken))
            throw EconomicAgentErrors.AgentNotFound(request.CounterpartyId);

        if (await _contractRepo.HasOverlappingAsync(resourceId, request.StartDate, request.TermMonths, tenantId, cancellationToken))
            throw EconomicContractErrors.OverlappingActiveContract(request.ResourceId, request.StartDate);

        var direction = Enumeration.FromDisplayName<ContractDirection>(request.Direction);
        var periodicity = Enumeration.FromDisplayName<Periodicity>(request.Periodicity);
        var currency = Enumeration.FromDisplayName<Currency>(request.Currency);
        var primaryPurpose = request.PrimaryPurpose is { } pp
            ? Enumeration.FromDisplayName<CommitmentPurpose>(pp)
            : CommitmentPurpose.Rent;
        var occurredAt = _timeProvider.GetUtcNow().UtcDateTime;

        var contract = EconomicContract.Create(
            EconomicContractId.New(),
            tenantId,
            counterpartyId,
            resourceId,
            direction,
            periodicity,
            request.AnchorDay,
            request.ExpectedAmount,
            currency,
            request.TermMonths,
            request.StartDate,
            occurredAt,
            primaryPurpose);

        foreach (var chargeModel in request.Charges ?? [])
        {
            var chargeResourceId = EconomicResourceId.From(chargeModel.ResourceId);
            var recipientId = EconomicAgentId.From(chargeModel.RecipientAgentId);

            if (!await _resourceRepo.ExistsAsync(chargeResourceId, tenantId, cancellationToken))
                throw EconomicResourceErrors.ResourceNotFound(chargeModel.ResourceId);
            if (!await _agentRepo.ExistsAsync(recipientId, tenantId, cancellationToken))
                throw EconomicAgentErrors.AgentNotFound(chargeModel.RecipientAgentId);

            var chargePurpose = Enumeration.FromDisplayName<CommitmentPurpose>(chargeModel.Purpose);
            var chargeCurrency = Enumeration.FromDisplayName<Currency>(chargeModel.Currency);

            contract.AddCharge(
                chargePurpose,
                chargeModel.ExpectedAmount,
                chargeCurrency,
                chargeResourceId,
                recipientId,
                chargeModel.CollectedByCounterparty,
                occurredAt);
        }

        await _contractRepo.InsertAsync(contract, cancellationToken);
        await _contractRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterEconomicContractResponse(
            contract.Id.Value,
            contract.Status.Name,
            contract.Direction.Name,
            contract.ResourceId.Value,
            contract.TermMonths,
            contract.StartDate);
    }
}

internal sealed class RegisterEconomicContractIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RegisterEconomicContractIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RegisterEconomicContractCommand, RegisterEconomicContractResponse>(mediator, requestManager, logger)
{
    protected override RegisterEconomicContractResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, string.Empty, Guid.Empty, 0, default);
}
