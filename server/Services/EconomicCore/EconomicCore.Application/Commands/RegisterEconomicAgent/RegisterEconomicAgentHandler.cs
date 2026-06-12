namespace EconomicCore.Application.Commands.RegisterEconomicAgent;

using EconomicCore.Application.Mediator;
using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicAgents.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

internal sealed class RegisterEconomicAgentHandler : IRequestHandler<RegisterEconomicAgentCommand, RegisterEconomicAgentResponse>
{
    private readonly IEconomicAgentRepository _agentRepo;
    private readonly TimeProvider _timeProvider;

    public RegisterEconomicAgentHandler(
        IEconomicAgentRepository agentRepo,
        TimeProvider timeProvider)
    {
        _agentRepo = agentRepo;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterEconomicAgentResponse> Handle(RegisterEconomicAgentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var scope = Enumeration.FromDisplayName<AgentScope>(request.Scope);
        var taxIdKind = string.IsNullOrWhiteSpace(request.TaxIdKind)
            ? null
            : Enumeration.FromDisplayName<TaxIdKind>(request.TaxIdKind);

        // O agregado compõe o TaxId internamente a partir dos primitivos.
        var agent = EconomicAgent.Create(
            EconomicAgentId.New(),
            tenantId,
            request.Name,
            scope,
            request.TaxIdValue,
            taxIdKind,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _agentRepo.InsertAsync(agent, cancellationToken);
        await _agentRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterEconomicAgentResponse(
            agent.Id.Value,
            agent.Name,
            agent.Scope.Name);
    }
}

internal sealed class RegisterEconomicAgentIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RegisterEconomicAgentIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RegisterEconomicAgentCommand, RegisterEconomicAgentResponse>(mediator, requestManager, logger)
{
    protected override RegisterEconomicAgentResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, string.Empty);
}
