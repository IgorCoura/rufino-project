namespace EconomicCore.Application.Commands.GenerateCommitments;

using EconomicCore.Application.Mediator;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

internal sealed class GenerateCommitmentsHandler : IRequestHandler<GenerateCommitmentsCommand, GenerateCommitmentsResponse>
{
    private readonly IEconomicContractRepository _contractRepo;

    public GenerateCommitmentsHandler(IEconomicContractRepository contractRepo)
    {
        _contractRepo = contractRepo;
    }

    public async Task<GenerateCommitmentsResponse> Handle(GenerateCommitmentsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var contractId = EconomicContractId.From(request.ContractId);

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        var period = new CompetencePeriod(request.Year, request.Month);
        var outflowId = CommitmentId.New();
        var inflowId = CommitmentId.New();

        contract.GenerateCommitmentsFor(period, outflowId, inflowId, request.OccurredAt);

        await _contractRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        var newCommitments = contract.Commitments
            .Where(c => c.Period.Equals(period))
            .Select(c => new CommitmentDto(c.Id.Value, c.Direction.Name, c.Status.Name, c.Period.Year, c.Period.Month))
            .ToList();

        return new GenerateCommitmentsResponse(newCommitments);
    }
}

internal sealed class GenerateCommitmentsIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<GenerateCommitmentsIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<GenerateCommitmentsCommand, GenerateCommitmentsResponse>(mediator, requestManager, logger)
{
    protected override GenerateCommitmentsResponse CreateResultForDuplicateRequest()
        => new([]);
}
