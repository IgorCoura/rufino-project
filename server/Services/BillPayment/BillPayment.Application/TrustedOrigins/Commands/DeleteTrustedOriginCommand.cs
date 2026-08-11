namespace BillPayment.Application.TrustedOrigins.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using Microsoft.Extensions.Logging;

/// <summary>
/// Remoção física. Uma decisão de confiança revogada não tem valor histórico próprio —
/// o que importa fica na trilha do Bill que a consumiu, via evidência do check.
/// </summary>
public sealed record DeleteTrustedOriginCommand(
    Guid TenantId,
    Guid TrustedOriginId) : IRequest<DeleteTrustedOriginResponse>;

public sealed record DeleteTrustedOriginResponse(Guid Id);

public sealed class DeleteTrustedOriginCommandHandler(
    ITrustedOriginRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTrustedOriginCommand, DeleteTrustedOriginResponse>
{
    public async Task<DeleteTrustedOriginResponse> Handle(
        DeleteTrustedOriginCommand request,
        CancellationToken cancellationToken)
    {
        var origin = await repository.GetAsync(
                TenantId.From(request.TenantId),
                TrustedOriginId.From(request.TrustedOriginId),
                cancellationToken)
            ?? throw TrustedOriginErrors.NotFound(request.TrustedOriginId);

        repository.Remove(origin);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new DeleteTrustedOriginResponse(request.TrustedOriginId);
    }
}

public sealed class DeleteTrustedOriginIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<DeleteTrustedOriginIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<DeleteTrustedOriginCommand, DeleteTrustedOriginResponse>(mediator, requestManager, logger)
{
    protected override DeleteTrustedOriginResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
