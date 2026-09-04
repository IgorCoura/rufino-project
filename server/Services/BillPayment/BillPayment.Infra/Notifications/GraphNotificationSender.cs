namespace BillPayment.Infra.Notifications;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BillPayment.Domain.Notifications;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Mailboxes.Graph;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Entrega o aviso por e-mail, pelo <c>sendMail</c> do Microsoft Graph.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Reaproveita o provedor de token da leitura de caixa, mas com credencial própria.</strong>
/// Ler a caixa de um cliente usa o registro de aplicativo dele; enviar aviso parte de nós, então
/// o remetente é da instalação. O que se compartilha é o mecanismo (client credentials + cache),
/// não o segredo.
/// </para>
/// <para>
/// <strong>Nenhum instrumento de pagamento atravessa</strong> — o contrato da porta já diz isso, e
/// aqui ele vale em dobro: o corpo do e-mail sai da nossa rede. O aviso diz o que aconteceu e para
/// onde ir; o dado fica atrás da autenticação.
/// </para>
/// <para>
/// <strong>Tenant sem destinatário não é falha.</strong> O canal é opt-in por tenant, e a maioria
/// não o configurou — devolver falha nesse caso encheria o log de erro sobre um estado normal.
/// </para>
/// </remarks>
internal sealed class GraphNotificationSender(
    IHttpClientFactory httpClientFactory,
    GraphTokenProvider tokenProvider,
    ITenantNotificationSettingsRepository settings,
    IOptions<NotificationOptions> options,
    IOptions<GraphOptions> graphOptions,
    ILogger<GraphNotificationSender> logger) : INotificationSender
{
    public const string CLIENT_NAME = "graph-notifications";

    private readonly NotificationOptions _options = options.Value;
    private readonly GraphOptions _graph = graphOptions.Value;

    public async Task SendAsync(
        TenantId tenantId,
        NotificationKind kind,
        NotificationPayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var recipients = await settings.FindForDeliveryAsync(tenantId, cancellationToken);

        if (recipients is not { CanDeliver: true })
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Aviso {Kind} do tenant {TenantId} sem destinatário configurado; nada foi enviado.",
                    kind,
                    tenantId.Value);
            }

            return;
        }

        var credential = new GraphMailboxCredential(
            _options.DirectoryId, _options.ClientId, _options.ClientSecret);

        var (token, failure) = await tokenProvider.AcquireAsync(credential, cancellationToken);

        if (token is null)
        {
            logger.LogWarning(
                "Aviso {Kind} não enviado: token do remetente indisponível ({Reason}).",
                kind,
                failure?.ReasonCode);

            return;
        }

        await PostAsync(tenantId, kind, payload, recipients.Recipients, token, cancellationToken);
    }

    private async Task PostAsync(
        TenantId tenantId,
        NotificationKind kind,
        NotificationPayload payload,
        IReadOnlyCollection<string> recipients,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var http = httpClientFactory.CreateClient(CLIENT_NAME);
        var url = $"{_graph.BaseUrl.TrimEnd('/')}/users/{_options.SenderAddress}/sendMail";

        var message = new SendMailRequest(
            new GraphMessage(
                payload.Title,
                new GraphBody("Text", BuildBody(payload)),
                [.. recipients.Select(r => new GraphRecipient(new GraphEmailAddress(r)))]),

            // Não guardar em "Itens enviados": a caixa remetente é de serviço, e acumular cópia
            // de todo aviso a transformaria num arquivo que ninguém lê e que cresce sozinho.
            SaveToSentItems: false);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(message, options: GraphHttp.Json),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await http.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Aviso {Kind} enviado para {Count} destinatário(s) do tenant {TenantId}.",
                    kind,
                    recipients.Count,
                    tenantId.Value);
            }

            return;
        }

        // Não lança: quem chama é um handler de Domain Event, e derrubá-lo faria o outbox
        // reentregar o mesmo alerta indefinidamente. O registro do alerta já existe no agregado.
        logger.LogWarning(
            "Aviso {Kind} recusado pelo provedor com status {Status}.",
            kind,
            (int)response.StatusCode);
    }

    private string BuildBody(NotificationPayload payload)
        => string.IsNullOrWhiteSpace(payload.ResourcePath) || string.IsNullOrWhiteSpace(_options.AppBaseUrl)
            ? payload.Body
            : $"{payload.Body}\n\n{_options.AppBaseUrl.TrimEnd('/')}{payload.ResourcePath}";

    private sealed record SendMailRequest(
        [property: JsonPropertyName("message")] GraphMessage Message,
        [property: JsonPropertyName("saveToSentItems")] bool SaveToSentItems);

    private sealed record GraphMessage(
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("body")] GraphBody Body,
        [property: JsonPropertyName("toRecipients")] IReadOnlyCollection<GraphRecipient> ToRecipients);

    private sealed record GraphBody(
        [property: JsonPropertyName("contentType")] string ContentType,
        [property: JsonPropertyName("content")] string Content);

    private sealed record GraphRecipient(
        [property: JsonPropertyName("emailAddress")] GraphEmailAddress EmailAddress);

    private sealed record GraphEmailAddress(
        [property: JsonPropertyName("address")] string Address);
}
