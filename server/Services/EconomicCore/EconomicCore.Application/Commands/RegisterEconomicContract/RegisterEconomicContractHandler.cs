namespace EconomicCore.Application.Commands.RegisterEconomicContract;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using MediatR;

internal sealed class RegisterEconomicContractHandler : IRequestHandler<RegisterEconomicContractCommand, RegisterEconomicContractResponse>
{
    private readonly IEconomicContractRepository _contractRepo;
    private readonly TimeProvider _timeProvider;

    public RegisterEconomicContractHandler(
        IEconomicContractRepository contractRepo,
        TimeProvider timeProvider)
    {
        _contractRepo = contractRepo;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterEconomicContractResponse> Handle(RegisterEconomicContractCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var direction = Enumeration.FromDisplayName<ContractDirection>(request.Direction);
        var periodicity = Enumeration.FromDisplayName<Periodicity>(request.Periodicity);
        var currency = Enumeration.FromDisplayName<Currency>(request.Currency);

        var recurrence = new RecurrencePattern(periodicity, request.AnchorDay);
        var expectedAmount = new Money(request.ExpectedAmount, currency);
        var defaultTerms = new CommitmentTerms(expectedAmount, tolerancePercent: 0m, windowDays: 30);

        var contract = EconomicContract.Create(
            EconomicContractId.New(),
            tenantId,
            EconomicAgentId.From(request.CounterpartyId),
            direction,
            recurrence,
            defaultTerms,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _contractRepo.InsertAsync(contract, cancellationToken);
        await _contractRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterEconomicContractResponse(
            contract.Id.Value,
            contract.Status.Name,
            contract.Direction.Name);
    }
}
