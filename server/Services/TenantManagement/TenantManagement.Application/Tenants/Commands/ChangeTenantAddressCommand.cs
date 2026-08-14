namespace TenantManagement.Application.Tenants.Commands;

using TenantManagement.Application.Mediator;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;

/// <summary>
/// Endereço tem comando próprio porque muda por conta própria e com frequência diferente da
/// razão social — juntá-los obrigaria o cliente a reenviar tudo para corrigir um número.
/// </summary>
public sealed record ChangeTenantAddressCommand(
    Guid TenantId,
    AddressInput Address) : IRequest<ChangeTenantAddressResponse>;

public sealed record ChangeTenantAddressResponse(Guid Id);

public sealed class ChangeTenantAddressCommandHandler(
    ITenantRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<ChangeTenantAddressCommand, ChangeTenantAddressResponse>
{
    public async Task<ChangeTenantAddressResponse> Handle(
        ChangeTenantAddressCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenant = await repository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken)
            ?? throw TenantErrors.NotFound(request.TenantId);

        var address = (request.Address ?? throw TenantErrors.AddressRequired()).ToAddress();

        tenant.ChangeAddress(address, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ChangeTenantAddressResponse(tenant.Id.Value);
    }
}

public sealed class ChangeTenantAddressIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ChangeTenantAddressIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ChangeTenantAddressCommand, ChangeTenantAddressResponse>(mediator, requestManager, logger)
{
    protected override ChangeTenantAddressResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
