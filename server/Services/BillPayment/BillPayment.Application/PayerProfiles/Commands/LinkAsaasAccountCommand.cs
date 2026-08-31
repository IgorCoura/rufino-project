namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Guarda a chave da subconta Asaas do tenant no cofre, com prova prévia — molde do
/// <c>ReplaceCaptureSourceCredential</c>: a chave crua chega uma vez (por isso
/// <see cref="ISensitiveCommand"/>), é provada contra o provedor, entra cifrada em
/// <c>tenant_secrets</c>, e só o ponteiro fica no <c>PayerProfile</c>. Prova reprovada
/// descarta a unidade de trabalho inteira — nada órfão no cofre.
/// </summary>
public sealed record LinkAsaasAccountCommand(
    Guid TenantId,
    string? ApiKey) : ITenantScopedCommand, IRequest<LinkAsaasAccountResponse>, ISensitiveCommand;

public sealed record LinkAsaasAccountResponse(Guid Id, bool CanSchedulePayments);

public sealed class LinkAsaasAccountCommandHandler(
    IPayerProfileRepository repository,
    IPaymentAccountVerifier verifier,
    ISecretVault vault,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LinkAsaasAccountCommand, LinkAsaasAccountResponse>
{
    public async Task<LinkAsaasAccountResponse> Handle(
        LinkAsaasAccountCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var apiKey = request.ApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            throw PayerProfileErrors.AsaasKeyRequired();

        var profile = await repository.GetByTenantAsync(tenantId, cancellationToken)
            ?? throw PayerProfileErrors.NotFound(request.TenantId);

        // A prova vem ANTES do cofre: aqui ela não precisa da referência (a chave está em mãos),
        // então provar primeiro poupa até o registro descartado da unidade de trabalho.
        var probe = await verifier.ProbeAsync(apiKey, cancellationToken);
        if (!probe.IsOk)
        {
            throw probe.IsRetryable
                ? PayerProfileErrors.AsaasProviderUnreachable(probe.ReasonCode!)
                : PayerProfileErrors.AsaasKeyRejected(probe.ReasonCode!);
        }

        var previous = profile.AsaasAccountRef;
        var credential = await vault.StoreAsync(
            tenantId, SecretKind.AsaasAccountApiKey, apiKey, cancellationToken);

        profile.LinkAsaasAccount(credential, clock.GetUtcNow().UtcDateTime);

        if (previous is not null)
            await vault.RemoveAsync(previous, cancellationToken);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new LinkAsaasAccountResponse(profile.Id.Value, profile.CanSchedulePayments);
    }
}

public sealed class LinkAsaasAccountIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<LinkAsaasAccountIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<LinkAsaasAccountCommand, LinkAsaasAccountResponse>(mediator, requestManager, logger)
{
    protected override LinkAsaasAccountResponse CreateResultForDuplicateRequest() => new(Guid.Empty, false);
}
