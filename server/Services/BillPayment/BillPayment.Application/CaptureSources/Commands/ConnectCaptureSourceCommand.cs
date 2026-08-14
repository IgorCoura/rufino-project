namespace BillPayment.Application.CaptureSources.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Conecta uma caixa monitorada a este tenant.
/// </summary>
/// <remarks>
/// <paramref name="Credential"/> é o material de autenticação <strong>opaco para a
/// Application</strong>: ela guarda no cofre e nunca o interpreta. O formato é contrato do
/// adapter do <c>Kind</c> escolhido e é fixado na sprint 2.2, junto com o Microsoft Graph.
/// </remarks>
public sealed record ConnectCaptureSourceCommand(
    Guid TenantId,
    string Kind,
    string DisplayName,
    string Address,
    string Credential,
    string? FolderPath) : IRequest<ConnectCaptureSourceResponse>, ISensitiveCommand;

/// <summary>
/// <paramref name="AlreadyMonitoredByAnotherAccount"/> é o aviso do ADR-008 — e é
/// <strong>booleano</strong> por decisão, não por simplicidade: sem id, sem nome, sem contagem.
/// Ele só existe depois de a prova de acesso passar, porque devolvê-lo antes transformaria o
/// endpoint num oráculo para descobrir que endereços estão cadastrados na plataforma.
/// </summary>
public sealed record ConnectCaptureSourceResponse(Guid Id, bool AlreadyMonitoredByAnotherAccount);

public sealed class ConnectCaptureSourceCommandHandler(
    ICaptureSourceRepository repository,
    ISecretVault vault,
    IMailboxReader mailboxReader,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConnectCaptureSourceCommand, ConnectCaptureSourceResponse>
{
    public async Task<ConnectCaptureSourceResponse> Handle(
        ConnectCaptureSourceCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var kind = Enumeration.FromDisplayName<CaptureSourceKind>(request.Kind);

        // A chave canônica vem do domínio — normalizar aqui de outro jeito faria a checagem de
        // duplicata divergir do índice único.
        var normalized = CaptureSource.Normalize(kind, request.Address);

        if (await repository.ExistsAsync(tenantId, normalized, cancellationToken))
            throw CaptureSourceErrors.AlreadyConnected(normalized);

        var now = clock.GetUtcNow();

        // O segredo entra no cofre antes da prova de acesso porque a prova precisa da referência.
        // Nada disso commita aqui: se a prova falhar, o throw desfaz a unidade de trabalho
        // inteira e não sobra credencial órfã no cofre.
        var credential = await vault.StoreAsync(
            tenantId, SecretKind.MailboxOAuthToken, request.Credential, cancellationToken);

        await EnsureMailboxIsReachableAsync(kind, normalized, credential, request.FolderPath, cancellationToken);

        var source = CaptureSource.Connect(
            tenantId, kind, request.DisplayName, request.Address, credential, now.UtcDateTime, request.FolderPath);

        await repository.AddAsync(source, cancellationToken);

        // Só agora, com o acesso provado, o aviso do ADR-008 pode ser consultado.
        var sharedWithAnotherAccount = await repository.IsAddressMonitoredByAnyTenantAsync(
            normalized, tenantId, cancellationToken);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ConnectCaptureSourceResponse(source.Id.Value, sharedWithAnotherAccount);
    }

    /// <summary>
    /// Substitui o "concluir o OAuth" do modelo de client credentials: sem tela de consentimento
    /// por fonte, quem prova que o usuário controla a caixa é uma leitura de teste.
    /// </summary>
    private async Task EnsureMailboxIsReachableAsync(
        CaptureSourceKind kind,
        string address,
        CredentialRef credential,
        string? folderPath,
        CancellationToken cancellationToken)
    {
        // Escolha de porta pelo Kind é orquestração, não regra: só a caixa de e-mail tem leitor.
        // Portal (fase 5) e ManualUpload não têm o que provar.
        if (kind != CaptureSourceKind.MicrosoftGraphMailbox)
            return;

        var probe = await mailboxReader.ProbeAccessAsync(address, credential, folderPath, cancellationToken);
        if (probe.IsOk)
            return;

        // "Recusou" e "não respondeu" pedem reações opostas do usuário — arrumar a credencial
        // versus tentar de novo — e por isso não colapsam no mesmo erro.
        throw probe.Status.IsRetryable
            ? CaptureSourceErrors.MailboxUnreachable(address, probe.ReasonCode!)
            : CaptureSourceErrors.MailboxAccessDenied(address, probe.ReasonCode!);
    }
}

public sealed class ConnectCaptureSourceIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ConnectCaptureSourceIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ConnectCaptureSourceCommand, ConnectCaptureSourceResponse>(mediator, requestManager, logger)
{
    protected override ConnectCaptureSourceResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, AlreadyMonitoredByAnotherAccount: false);
}
