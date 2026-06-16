namespace EconomicCore.Application.Commands.ChangeContractPenaltyTerms;

using EconomicCore.Application.Mediator;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

internal sealed class ChangeContractPenaltyTermsHandler : IRequestHandler<ChangeContractPenaltyTermsCommand, ChangeContractPenaltyTermsResponse>
{
    private readonly IEconomicContractRepository _contractRepo;
    private readonly TimeProvider _timeProvider;

    public ChangeContractPenaltyTermsHandler(IEconomicContractRepository contractRepo, TimeProvider timeProvider)
    {
        _contractRepo = contractRepo;
        _timeProvider = timeProvider;
    }

    public async Task<ChangeContractPenaltyTermsResponse> Handle(ChangeContractPenaltyTermsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var contractId = EconomicContractId.From(request.ContractId);
        var fineKind = Enumeration.FromDisplayName<PenaltyValueKind>(request.FineKind);
        var interestKind = Enumeration.FromDisplayName<PenaltyValueKind>(request.InterestKind);
        var interestPeriod = Enumeration.FromDisplayName<InterestAccrualPeriod>(request.InterestPeriod);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        contract.ChangePenaltyPolicy(fineKind, request.FineValue, interestKind, request.InterestValue, interestPeriod, now);

        await _contractRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ChangeContractPenaltyTermsResponse(
            contract.Id.Value,
            contract.PenaltyPolicy.FineKind.Name,
            contract.PenaltyPolicy.FineValue,
            contract.PenaltyPolicy.InterestKind.Name,
            contract.PenaltyPolicy.InterestValue,
            contract.PenaltyPolicy.InterestPeriod.Name);
    }
}

internal sealed class ChangeContractPenaltyTermsIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ChangeContractPenaltyTermsIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ChangeContractPenaltyTermsCommand, ChangeContractPenaltyTermsResponse>(mediator, requestManager, logger)
{
    protected override ChangeContractPenaltyTermsResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, 0m, string.Empty, 0m, string.Empty);
}
