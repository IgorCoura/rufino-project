namespace BillPayment.Application.Payees.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record RemovePayeeAliasCommand(
    Guid TenantId,
    Guid PayeeId,
    string Alias) : ITenantScopedCommand, IRequest<RemovePayeeAliasResponse>;

public sealed record RemovePayeeAliasResponse(Guid Id);

public sealed class RemovePayeeAliasCommandHandler(
    IPayeeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemovePayeeAliasCommand, RemovePayeeAliasResponse>
{
    public async Task<RemovePayeeAliasResponse> Handle(
        RemovePayeeAliasCommand request,
        CancellationToken cancellationToken)
    {
        var payee = await repository.GetAsync(
                TenantId.From(request.TenantId),
                PayeeId.From(request.PayeeId),
                cancellationToken)
            ?? throw PayeeErrors.NotFound(request.PayeeId);

        payee.ForgetAlias(request.Alias, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RemovePayeeAliasResponse(payee.Id.Value);
    }
}

public sealed class RemovePayeeAliasIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RemovePayeeAliasIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RemovePayeeAliasCommand, RemovePayeeAliasResponse>(mediator, requestManager, logger)
{
    protected override RemovePayeeAliasResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
