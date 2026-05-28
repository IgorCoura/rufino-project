namespace EconomicCore.Application.Commands.RegisterEconomicAgent;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicAgents.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using MediatR;

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

        TaxId? taxId = null;
        if (!string.IsNullOrWhiteSpace(request.TaxIdValue) && !string.IsNullOrWhiteSpace(request.TaxIdKind))
        {
            var kind = Enumeration.FromDisplayName<TaxIdKind>(request.TaxIdKind);
            taxId = new TaxId(request.TaxIdValue, kind);
        }

        var agent = EconomicAgent.Create(
            EconomicAgentId.New(),
            tenantId,
            request.Name,
            scope,
            taxId,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _agentRepo.InsertAsync(agent, cancellationToken);
        await _agentRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterEconomicAgentResponse(
            agent.Id.Value,
            agent.Name,
            agent.Scope.Name);
    }
}
