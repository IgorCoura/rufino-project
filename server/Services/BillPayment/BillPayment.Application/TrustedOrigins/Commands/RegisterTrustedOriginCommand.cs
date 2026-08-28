namespace BillPayment.Application.TrustedOrigins.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using Microsoft.Extensions.Logging;

public sealed record RegisterTrustedOriginCommand(
    Guid TenantId,
    string Kind,
    string Value,
    string Decision,
    Guid DecidedBy,
    string? Note) : ITenantScopedCommand, IRequest<RegisterTrustedOriginResponse>;

public sealed record RegisterTrustedOriginResponse(Guid Id);

public sealed class RegisterTrustedOriginCommandHandler(
    ITrustedOriginRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterTrustedOriginCommand, RegisterTrustedOriginResponse>
{
    public async Task<RegisterTrustedOriginResponse> Handle(
        RegisterTrustedOriginCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        // Tradução de input: converter string em Smart Enum é responsabilidade do handler.
        var kind = Enumeration.FromDisplayName<OriginKind>(request.Kind);
        var decision = Enumeration.FromDisplayName<TrustDecision>(request.Decision);

        // A chave canônica vem do domínio — normalizar aqui de outro jeito faria a
        // checagem de duplicata divergir do índice único.
        var normalized = TrustedOrigin.Normalize(request.Value);

        if (await repository.ExistsAsync(tenantId, kind, normalized, cancellationToken))
            throw TrustedOriginErrors.AlreadyRegistered(kind.Name, normalized);

        var origin = TrustedOrigin.Register(
            tenantId,
            kind,
            request.Value,
            decision,
            UserId.From(request.DecidedBy),
            request.Note,
            DateTime.UtcNow);

        await repository.AddAsync(origin, cancellationToken);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterTrustedOriginResponse(origin.Id.Value);
    }
}

public sealed class RegisterTrustedOriginIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RegisterTrustedOriginIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RegisterTrustedOriginCommand, RegisterTrustedOriginResponse>(mediator, requestManager, logger)
{
    protected override RegisterTrustedOriginResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
