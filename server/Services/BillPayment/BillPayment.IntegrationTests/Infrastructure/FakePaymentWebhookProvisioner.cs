namespace BillPayment.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;

/// <summary>
/// O provedor de webhook, determinístico. Cada teste arma o desfecho e confere o que chegou lá.
/// </summary>
/// <remarks>
/// A tradução do JSON real do provedor é assunto do adapter; aqui o que está sob teste é o
/// caminho vínculo → evento → outbox → provisionamento → cofre → perfil, e a varredura que o
/// reconstrói quando esse caminho falha.
/// </remarks>
internal sealed class FakePaymentWebhookProvisioner : IPaymentWebhookProvisioner
{
    private int _created;

    /// <summary>Uma entrada por chamada de <see cref="EnsureAsync"/>, na ordem em que aconteceram.</summary>
    public ConcurrentQueue<EnsureCall> EnsureCalls { get; } = new();

    /// <summary>Os ids de webhook que passaram por <see cref="RemoveAsync"/>.</summary>
    public ConcurrentQueue<string> RemovedWebhookIds { get; } = new();

    /// <summary>Armado por teste; <c>null</c> devolve um webhook novo com id sequencial.</summary>
    public WebhookProvisioningResult? NextEnsure { get; set; }

    /// <summary>O que <see cref="GetHealthAsync"/> responde. Por padrão, entregando na URL pedida.</summary>
    public WebhookHealthResult? NextHealth { get; set; }

    /// <summary>Desfecho de <see cref="RemoveAsync"/>.</summary>
    public bool NextRemoveSucceeds { get; set; } = true;

    public void Reset()
    {
        EnsureCalls.Clear();
        RemovedWebhookIds.Clear();
        NextEnsure = null;
        NextHealth = null;
        NextRemoveSucceeds = true;
        _created = 0;
    }

    public Task<WebhookProvisioningResult> EnsureAsync(
        CredentialRef? credential,
        string callbackUrl,
        string notificationEmail,
        string? authToken,
        CancellationToken cancellationToken)
    {
        EnsureCalls.Enqueue(new EnsureCall(callbackUrl, notificationEmail, authToken));

        if (NextEnsure is { } armed)
            return Task.FromResult(armed);

        // Espelha o adapter real: token nulo é rotação (gera um novo), token vindo de fora é cura
        // (devolve o mesmo). É essa distinção que os testes de rotação e de cura verificam.
        var id = $"wh_{Interlocked.Increment(ref _created):D3}";
        var token = authToken ?? $"tok_{Guid.NewGuid():N}";

        return Task.FromResult(WebhookProvisioningResult.Provisioned(id, token));
    }

    public Task<WebhookHealthResult> GetHealthAsync(
        CredentialRef? credential,
        string providerWebhookId,
        CancellationToken cancellationToken)
        => Task.FromResult(NextHealth ?? WebhookHealthResult.NotFound());

    public Task<bool> RemoveAsync(
        CredentialRef? credential,
        string providerWebhookId,
        CancellationToken cancellationToken)
    {
        RemovedWebhookIds.Enqueue(providerWebhookId);
        return Task.FromResult(NextRemoveSucceeds);
    }

    /// <param name="AuthToken">
    /// <c>null</c> significa que quem chamou pediu ROTAÇÃO; qualquer valor significa que pediu
    /// para manter o token vigente. É a asserção central dos testes de cura.
    /// </param>
    internal sealed record EnsureCall(string CallbackUrl, string NotificationEmail, string? AuthToken);
}
