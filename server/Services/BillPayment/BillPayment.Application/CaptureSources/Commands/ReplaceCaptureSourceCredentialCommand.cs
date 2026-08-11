namespace BillPayment.Application.CaptureSources.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Aponta a fonte para outra credencial — rotação do segredo, ou reconexão depois de revogação.
/// </summary>
public sealed record ReplaceCaptureSourceCredentialCommand(
    Guid TenantId,
    Guid CaptureSourceId,
    string Credential) : IRequest<ReplaceCaptureSourceCredentialResponse>;

public sealed record ReplaceCaptureSourceCredentialResponse(Guid Id);

public sealed class ReplaceCaptureSourceCredentialCommandHandler(
    ICaptureSourceRepository repository,
    ISecretVault vault,
    IMailboxReader mailboxReader,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReplaceCaptureSourceCredentialCommand, ReplaceCaptureSourceCredentialResponse>
{
    public async Task<ReplaceCaptureSourceCredentialResponse> Handle(
        ReplaceCaptureSourceCredentialCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var id = CaptureSourceId.From(request.CaptureSourceId);

        var source = await repository.GetAsync(tenantId, id, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(request.CaptureSourceId);

        var previous = source.Credential;

        // Referência NOVA em vez de ReplaceAsync sobre a antiga: se a prova de acesso reprovar,
        // a unidade de trabalho inteira é descartada e a credencial que ainda funcionava
        // permanece intacta. Sobrescrever no lugar destruiria a boa antes de provar a nova.
        var credential = await vault.StoreAsync(
            tenantId, SecretKind.MailboxOAuthToken, request.Credential, cancellationToken);

        if (source.Kind == CaptureSourceKind.MicrosoftGraphMailbox)
        {
            var probe = await mailboxReader.ProbeAccessAsync(
                source.Address, credential, source.FolderPath, cancellationToken);
            if (!probe.IsOk)
            {
                throw probe.Status.IsRetryable
                    ? CaptureSourceErrors.MailboxUnreachable(source.Address, probe.ReasonCode!)
                    : CaptureSourceErrors.MailboxAccessDenied(source.Address, probe.ReasonCode!);
            }
        }

        source.ReplaceCredential(credential, clock.GetUtcNow().UtcDateTime);

        if (previous is not null)
            await vault.RemoveAsync(previous, cancellationToken);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ReplaceCaptureSourceCredentialResponse(source.Id.Value);
    }
}

public sealed class ReplaceCaptureSourceCredentialIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ReplaceCaptureSourceCredentialIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ReplaceCaptureSourceCredentialCommand, ReplaceCaptureSourceCredentialResponse>(
        mediator, requestManager, logger)
{
    protected override ReplaceCaptureSourceCredentialResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
