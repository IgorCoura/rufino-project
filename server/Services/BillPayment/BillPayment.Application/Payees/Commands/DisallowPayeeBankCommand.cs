namespace BillPayment.Application.Payees.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record DisallowPayeeBankCommand(
    Guid TenantId,
    Guid PayeeId,
    string BankCode) : IRequest<DisallowPayeeBankResponse>;

public sealed record DisallowPayeeBankResponse(Guid Id);

public sealed class DisallowPayeeBankCommandHandler(
    IPayeeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DisallowPayeeBankCommand, DisallowPayeeBankResponse>
{
    public async Task<DisallowPayeeBankResponse> Handle(
        DisallowPayeeBankCommand request,
        CancellationToken cancellationToken)
    {
        var payee = await repository.GetAsync(
                TenantId.From(request.TenantId),
                PayeeId.From(request.PayeeId),
                cancellationToken)
            ?? throw PayeeErrors.NotFound(request.PayeeId);

        payee.DisallowBank(request.BankCode, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new DisallowPayeeBankResponse(payee.Id.Value);
    }
}

public sealed class DisallowPayeeBankIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<DisallowPayeeBankIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<DisallowPayeeBankCommand, DisallowPayeeBankResponse>(mediator, requestManager, logger)
{
    protected override DisallowPayeeBankResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
