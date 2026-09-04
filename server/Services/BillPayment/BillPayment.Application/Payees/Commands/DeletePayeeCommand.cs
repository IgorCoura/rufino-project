namespace BillPayment.Application.Payees.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Remoção física. É segura enquanto <c>Bill</c> não existe; quando existir, boleto já pago
/// vai referenciar o beneficiário e a operação precisa virar desativação — o caminho para
/// isso já está pronto em <c>AlterPayeeActivationCommand</c>.
/// </summary>
public sealed record DeletePayeeCommand(
    Guid TenantId,
    Guid PayeeId) : ITenantScopedCommand, IRequest<DeletePayeeResponse>;

public sealed record DeletePayeeResponse(Guid Id);

public sealed class DeletePayeeCommandHandler(
    IPayeeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePayeeCommand, DeletePayeeResponse>
{
    public async Task<DeletePayeeResponse> Handle(
        DeletePayeeCommand request,
        CancellationToken cancellationToken)
    {
        var payee = await repository.GetAsync(
                TenantId.From(request.TenantId),
                PayeeId.From(request.PayeeId),
                cancellationToken)
            ?? throw PayeeErrors.NotFound(request.PayeeId);

        repository.Remove(payee);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new DeletePayeeResponse(request.PayeeId);
    }
}

public sealed class DeletePayeeIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<DeletePayeeIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<DeletePayeeCommand, DeletePayeeResponse>(mediator, requestManager, logger)
{
    protected override DeletePayeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
