namespace BillPayment.Application.Payees.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record AlterPayeeAmountPolicyCommand(
    Guid TenantId,
    Guid PayeeId,
    string AmountPolicyKind,
    decimal? ExpectedAmount,
    decimal? TolerancePercent,
    decimal? MinAmount,
    decimal? MaxAmount) : IRequest<AlterPayeeAmountPolicyResponse>;

public sealed record AlterPayeeAmountPolicyResponse(Guid Id);

public sealed class AlterPayeeAmountPolicyCommandHandler(
    IPayeeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AlterPayeeAmountPolicyCommand, AlterPayeeAmountPolicyResponse>
{
    public async Task<AlterPayeeAmountPolicyResponse> Handle(
        AlterPayeeAmountPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var policyKind = Enumeration.FromDisplayName<AmountPolicyKind>(request.AmountPolicyKind);

        var payee = await repository.GetAsync(
                TenantId.From(request.TenantId),
                PayeeId.From(request.PayeeId),
                cancellationToken)
            ?? throw PayeeErrors.NotFound(request.PayeeId);

        payee.ChangeAmountPolicy(
            policyKind,
            request.ExpectedAmount,
            request.TolerancePercent,
            request.MinAmount,
            request.MaxAmount,
            DateTime.UtcNow);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new AlterPayeeAmountPolicyResponse(payee.Id.Value);
    }
}

public sealed class AlterPayeeAmountPolicyIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<AlterPayeeAmountPolicyIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<AlterPayeeAmountPolicyCommand, AlterPayeeAmountPolicyResponse>(mediator, requestManager, logger)
{
    protected override AlterPayeeAmountPolicyResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
