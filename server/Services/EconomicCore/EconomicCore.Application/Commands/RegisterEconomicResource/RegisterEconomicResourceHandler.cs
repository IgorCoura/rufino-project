namespace EconomicCore.Application.Commands.RegisterEconomicResource;

using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Operational.EconomicResources.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using MediatR;

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
