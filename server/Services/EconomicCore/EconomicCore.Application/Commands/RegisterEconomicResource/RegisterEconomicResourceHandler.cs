namespace EconomicCore.Application.Commands.RegisterEconomicResource;

using EconomicCore.Application.Mediator;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Operational.EconomicResources.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

internal sealed class RegisterEconomicResourceHandler : IRequestHandler<RegisterEconomicResourceCommand, RegisterEconomicResourceResponse>
{
    private readonly IEconomicResourceRepository _resourceRepo;
    private readonly TimeProvider _timeProvider;

    public RegisterEconomicResourceHandler(
        IEconomicResourceRepository resourceRepo,
        TimeProvider timeProvider)
    {
        _resourceRepo = resourceRepo;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterEconomicResourceResponse> Handle(RegisterEconomicResourceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var kind = Enumeration.FromDisplayName<ResourceKind>(request.Kind);

        var resource = EconomicResource.Create(
            EconomicResourceId.New(),
            tenantId,
            request.Name,
            kind,
            typeId: null,
            custodianId: null,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _resourceRepo.InsertAsync(resource, cancellationToken);
        await _resourceRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterEconomicResourceResponse(
            resource.Id.Value,
            resource.Name,
            resource.Kind.Name);
    }
}

internal sealed class RegisterEconomicResourceIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RegisterEconomicResourceIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RegisterEconomicResourceCommand, RegisterEconomicResourceResponse>(mediator, requestManager, logger)
{
    protected override RegisterEconomicResourceResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, string.Empty);
}
