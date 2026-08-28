namespace BillPayment.Application.Notifications.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Notifications;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Define para quem os avisos de expectativa vão, e se o canal externo está ligado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Substitui a lista inteira</strong>, porque é assim que a tela funciona: quem edita vê
/// a lista completa e a devolve completa. Um par adicionar/remover exigiria que o cliente
/// reconciliasse a diferença, e a reconciliação errada tira do ar o único canal de aviso.
/// </para>
/// <para>
/// <strong>Cria a configuração na primeira chamada</strong> — não há endpoint separado de
/// criação. Um por tenant, e exigir "criar antes de editar" só produziria um 404 que o cliente
/// teria de traduzir num POST.
/// </para>
/// </remarks>
public sealed record ConfigureTenantNotificationsCommand(
    Guid TenantId,
    IReadOnlyCollection<string> Recipients,
    bool IsEnabled) : IRequest<ConfigureTenantNotificationsResponse>, ISensitiveCommand;

public sealed record ConfigureTenantNotificationsResponse(Guid Id, int RecipientCount, bool IsEnabled);

public sealed class ConfigureTenantNotificationsCommandHandler(
    ITenantNotificationSettingsRepository settings,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfigureTenantNotificationsCommand, ConfigureTenantNotificationsResponse>
{
    public async Task<ConfigureTenantNotificationsResponse> Handle(
        ConfigureTenantNotificationsCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var now = clock.GetUtcNow().UtcDateTime;

        var existing = await settings.GetAsync(tenantId, cancellationToken);

        if (existing is null)
        {
            existing = TenantNotificationSettings.Create(tenantId, now);
            await settings.AddAsync(existing, cancellationToken);
        }

        existing.Configure(request.Recipients ?? [], request.IsEnabled, now);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ConfigureTenantNotificationsResponse(
            existing.Id.Value, existing.Recipients.Count, existing.IsEnabled);
    }
}

public sealed class ConfigureTenantNotificationsIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ConfigureTenantNotificationsIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ConfigureTenantNotificationsCommand, ConfigureTenantNotificationsResponse>(
        mediator, requestManager, logger)
{
    protected override ConfigureTenantNotificationsResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, 0, false);
}
