namespace BillPayment.Application.Payees.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record AlterPayeeActivationCommand(
    Guid TenantId,
    Guid PayeeId,
    bool IsActive) : IRequest<AlterPayeeActivationResponse>;

public sealed record AlterPayeeActivationResponse(Guid Id);

public sealed class AlterPayeeActivationCommandHandler(
    IPayeeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AlterPayeeActivationCommand, AlterPayeeActivationResponse>
{
    public async Task<AlterPayeeActivationResponse> Handle(
        AlterPayeeActivationCommand request,
        CancellationToken cancellationToken)
    {
        var payee = await repository.GetAsync(
                TenantId.From(request.TenantId),
                PayeeId.From(request.PayeeId),
                cancellationToken)
            ?? throw PayeeErrors.NotFound(request.PayeeId);

        payee.SetActivation(request.IsActive, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new AlterPayeeActivationResponse(payee.Id.Value);
    }
}

public sealed class AlterPayeeActivationIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<AlterPayeeActivationIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<AlterPayeeActivationCommand, AlterPayeeActivationResponse>(mediator, requestManager, logger)
{
    protected override AlterPayeeActivationResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
