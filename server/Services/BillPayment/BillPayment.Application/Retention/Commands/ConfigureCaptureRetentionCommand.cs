namespace BillPayment.Application.Retention.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Retention;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Liga ou desliga a purga do livro-caixa e escolhe o prazo.
/// </summary>
/// <remarks>
/// Cria a política quando o tenant nunca configurou — não existe "configurar antes de criar" do
/// lado de fora, e obrigar um POST antes do PUT só transferiria essa costura para a tela.
/// </remarks>
public sealed record ConfigureCaptureRetentionCommand(Guid TenantId, bool IsEnabled, int WindowDays)
    : IRequest<ConfigureCaptureRetentionResponse>;

public sealed record ConfigureCaptureRetentionResponse(Guid Id, bool IsEnabled, int WindowDays);

public sealed class ConfigureCaptureRetentionCommandHandler(
    ICaptureRetentionPolicyRepository repository,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfigureCaptureRetentionCommand, ConfigureCaptureRetentionResponse>
{
    public async Task<ConfigureCaptureRetentionResponse> Handle(
        ConfigureCaptureRetentionCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var now = clock.GetUtcNow().UtcDateTime;

        // Quem valida o prazo é o domínio: a faixa é fechada, e um número livre viraria retenção
        // arbitrária — inclusive zero, que apagaria o histórico no instante em que ele nasce.
        var window = CaptureRetentionPolicy.WindowFromDays(request.WindowDays);

        var policy = await repository.GetAsync(tenantId, cancellationToken);

        if (policy is null)
        {
            policy = CaptureRetentionPolicy.Default(tenantId, now);
            await repository.AddAsync(policy, cancellationToken);
        }

        policy.Configure(request.IsEnabled, window, now);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ConfigureCaptureRetentionResponse(policy.Id.Value, policy.IsEnabled, policy.Window.Days);
    }
}

public sealed class ConfigureCaptureRetentionIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ConfigureCaptureRetentionIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ConfigureCaptureRetentionCommand, ConfigureCaptureRetentionResponse>(
        mediator, requestManager, logger)
{
    protected override ConfigureCaptureRetentionResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, false, RetentionWindow.Default.Days);
}
