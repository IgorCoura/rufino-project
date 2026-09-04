namespace BillPayment.Application.Payees.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record AddPayeeAliasCommand(
    Guid TenantId,
    Guid PayeeId,
    string Alias) : ITenantScopedCommand, IRequest<AddPayeeAliasResponse>;

public sealed record AddPayeeAliasResponse(Guid Id);

public sealed class AddPayeeAliasCommandHandler(
    IPayeeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddPayeeAliasCommand, AddPayeeAliasResponse>
{
    public async Task<AddPayeeAliasResponse> Handle(
        AddPayeeAliasCommand request,
        CancellationToken cancellationToken)
    {
        var payee = await repository.GetAsync(
                TenantId.From(request.TenantId),
                PayeeId.From(request.PayeeId),
                cancellationToken)
            ?? throw PayeeErrors.NotFound(request.PayeeId);

        payee.LearnAlias(request.Alias, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new AddPayeeAliasResponse(payee.Id.Value);
    }
}

public sealed class AddPayeeAliasIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<AddPayeeAliasIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<AddPayeeAliasCommand, AddPayeeAliasResponse>(mediator, requestManager, logger)
{
    protected override AddPayeeAliasResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
