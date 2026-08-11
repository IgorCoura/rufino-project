namespace BillPayment.Application.Payees.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record RenamePayeeCommand(
    Guid TenantId,
    Guid PayeeId,
    string LegalName) : IRequest<RenamePayeeResponse>;

public sealed record RenamePayeeResponse(Guid Id);

public sealed class RenamePayeeCommandHandler(
    IPayeeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RenamePayeeCommand, RenamePayeeResponse>
{
    public async Task<RenamePayeeResponse> Handle(
        RenamePayeeCommand request,
        CancellationToken cancellationToken)
    {
        var payee = await repository.GetAsync(
                TenantId.From(request.TenantId),
                PayeeId.From(request.PayeeId),
                cancellationToken)
            ?? throw PayeeErrors.NotFound(request.PayeeId);

        payee.Rename(request.LegalName, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RenamePayeeResponse(payee.Id.Value);
    }
}

public sealed class RenamePayeeIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RenamePayeeIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RenamePayeeCommand, RenamePayeeResponse>(mediator, requestManager, logger)
{
    protected override RenamePayeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
