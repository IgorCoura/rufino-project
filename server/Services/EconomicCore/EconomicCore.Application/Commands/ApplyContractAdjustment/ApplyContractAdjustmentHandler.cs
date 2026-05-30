namespace EconomicCore.Application.Commands.ApplyContractAdjustment;

using EconomicCore.Application.Mediator;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

internal sealed class ApplyContractAdjustmentHandler : IRequestHandler<ApplyContractAdjustmentCommand, ApplyContractAdjustmentResponse>
{
    private readonly IEconomicContractRepository _contractRepo;
    private readonly TimeProvider _timeProvider;

    public ApplyContractAdjustmentHandler(IEconomicContractRepository contractRepo, TimeProvider timeProvider)
    {
        _contractRepo = contractRepo;
        _timeProvider = timeProvider;
    }

    public async Task<ApplyContractAdjustmentResponse> Handle(ApplyContractAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var contractId = EconomicContractId.From(request.ContractId);
        var purpose = Enumeration.FromDisplayName<CommitmentPurpose>(request.Purpose);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        // O Handler só escolhe a operação pelo input (XOR valor absoluto vs índice); a precificação e a
        // composição de Money vivem no agregado (ApplyAdjustmentToAmount / ApplyAdjustmentByRate).
        Money applied;
        if (request.NewAmount is { } absolute && request.IndexRate is null)
            applied = contract.ApplyAdjustmentToAmount(
                purpose, request.EffectiveFromYear, request.EffectiveFromMonth, absolute,
                Enumeration.FromDisplayName<Currency>(request.Currency), now);
        else if (request.IndexRate is { } rate && request.NewAmount is null)
            applied = contract.ApplyAdjustmentByRate(
                purpose, request.EffectiveFromYear, request.EffectiveFromMonth, rate, now);
        else
            throw EconomicContractErrors.AmbiguousAdjustment();

        await _contractRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ApplyContractAdjustmentResponse(contract.Id.Value, purpose.Name, applied.Amount);
    }
}

internal sealed class ApplyContractAdjustmentIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ApplyContractAdjustmentIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ApplyContractAdjustmentCommand, ApplyContractAdjustmentResponse>(mediator, requestManager, logger)
{
    protected override ApplyContractAdjustmentResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, 0m);
}
