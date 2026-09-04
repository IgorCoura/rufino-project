namespace TenantManagement.Application.Tenants.Commands;

using TenantManagement.Application.Mediator;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;

/// <summary>
/// Habilitar e desabilitar produto são o mesmo assunto e andam juntos — ficam no mesmo
/// arquivo para que a simetria entre os dois seja visível de um golpe de vista.
/// </summary>
public sealed record ActivateTenantProductCommand(
    Guid TenantId,
    string Product) : IRequest<TenantProductResponse>;

public sealed record DeactivateTenantProductCommand(
    Guid TenantId,
    string Product) : IRequest<TenantProductResponse>;

public sealed record TenantProductResponse(Guid Id, string Product, bool IsActive);

public sealed class ActivateTenantProductCommandHandler(
    ITenantRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<ActivateTenantProductCommand, TenantProductResponse>
{
    public async Task<TenantProductResponse> Handle(
        ActivateTenantProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = TenantInput.ParseProduct(request.Product);

        var tenant = await repository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken)
            ?? throw TenantErrors.NotFound(request.TenantId);

        tenant.ActivateProduct(product, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new TenantProductResponse(tenant.Id.Value, product.Name, true);
    }
}

public sealed class DeactivateTenantProductCommandHandler(
    ITenantRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<DeactivateTenantProductCommand, TenantProductResponse>
{
    public async Task<TenantProductResponse> Handle(
        DeactivateTenantProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = TenantInput.ParseProduct(request.Product);

        var tenant = await repository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken)
            ?? throw TenantErrors.NotFound(request.TenantId);

        tenant.DeactivateProduct(product, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new TenantProductResponse(tenant.Id.Value, product.Name, false);
    }
}

public sealed class ActivateTenantProductIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ActivateTenantProductIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ActivateTenantProductCommand, TenantProductResponse>(mediator, requestManager, logger)
{
    protected override TenantProductResponse CreateResultForDuplicateRequest() => new(Guid.Empty, string.Empty, false);
}

public sealed class DeactivateTenantProductIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<DeactivateTenantProductIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<DeactivateTenantProductCommand, TenantProductResponse>(mediator, requestManager, logger)
{
    protected override TenantProductResponse CreateResultForDuplicateRequest() => new(Guid.Empty, string.Empty, false);
}
