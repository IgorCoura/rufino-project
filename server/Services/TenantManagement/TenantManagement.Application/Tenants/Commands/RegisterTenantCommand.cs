namespace TenantManagement.Application.Tenants.Commands;

using TenantManagement.Application.Mediator;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cadastra pessoa física ou jurídica. <paramref name="Id"/> é opcional e existe para a
/// migração de cadastros que já têm identidade em outro lugar — informar o Id preserva o
/// acesso que já foi emitido em vez de obrigar a reemitir tudo.
/// </summary>
public sealed record RegisterTenantCommand(
    string Kind,
    string LegalName,
    string? TradeName,
    string PrimaryTaxId,
    string ContactEmail,
    string? ContactPhone,
    AddressInput Address,
    string OwnerEmail,
    IReadOnlyList<string>? Products,
    Guid? Id = null) : IRequest<RegisterTenantResponse>;

public sealed record RegisterTenantResponse(Guid Id);

public sealed class RegisterTenantCommandHandler(
    ITenantRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<RegisterTenantCommand, RegisterTenantResponse>
{
    public async Task<RegisterTenantResponse> Handle(
        RegisterTenantCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var kind = TenantInput.ParseKind(request.Kind);
        var taxId = TaxId.Parse(request.PrimaryTaxId);
        var occurredAt = timeProvider.GetUtcNow().UtcDateTime;

        if (await repository.ExistsByPrimaryTaxIdAsync(taxId, cancellationToken))
            throw TenantErrors.TaxIdAlreadyRegistered(taxId.Formatted());

        var id = request.Id is { } informed && informed != Guid.Empty
            ? TenantId.From(informed)
            : TenantId.New();

        if (await repository.ExistsAsync(id, cancellationToken))
            throw TenantErrors.TaxIdAlreadyRegistered(taxId.Formatted());

        var tenant = Tenant.Register(
            id,
            kind,
            request.LegalName,
            request.TradeName,
            taxId,
            ContactInfo.Create(request.ContactEmail, request.ContactPhone),
            (request.Address ?? throw TenantErrors.AddressRequired()).ToAddress(),
            request.OwnerEmail,
            occurredAt);

        var products = request.Products ?? [];

        tenant.ActivateProductRange(products.Select(x => TenantInput.ParseProduct(x)), occurredAt);


        await repository.AddAsync(tenant, cancellationToken);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterTenantResponse(tenant.Id.Value);
    }
}

public sealed class RegisterTenantIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RegisterTenantIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RegisterTenantCommand, RegisterTenantResponse>(mediator, requestManager, logger)
{
    protected override RegisterTenantResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
