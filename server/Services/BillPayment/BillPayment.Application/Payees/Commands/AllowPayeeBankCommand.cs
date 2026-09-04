namespace BillPayment.Application.Payees.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record AllowPayeeBankCommand(
    Guid TenantId,
    Guid PayeeId,
    string BankCode) : ITenantScopedCommand, IRequest<AllowPayeeBankResponse>;

public sealed record AllowPayeeBankResponse(Guid Id);

public sealed class AllowPayeeBankCommandHandler(
    IPayeeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AllowPayeeBankCommand, AllowPayeeBankResponse>
{
    public async Task<AllowPayeeBankResponse> Handle(
        AllowPayeeBankCommand request,
        CancellationToken cancellationToken)
    {
        var payee = await repository.GetAsync(
                TenantId.From(request.TenantId),
                PayeeId.From(request.PayeeId),
                cancellationToken)
            ?? throw PayeeErrors.NotFound(request.PayeeId);

        payee.AllowBank(request.BankCode, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new AllowPayeeBankResponse(payee.Id.Value);
    }
}

public sealed class AllowPayeeBankIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<AllowPayeeBankIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<AllowPayeeBankCommand, AllowPayeeBankResponse>(mediator, requestManager, logger)
{
    protected override AllowPayeeBankResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
