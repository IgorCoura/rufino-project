namespace BillPayment.Infra.Mailboxes.Graph;

using System.Text.Json.Serialization;

/// <summary>
/// Os DTOs do Graph, deliberadamente <strong>frouxos</strong>.
/// </summary>
/// <remarks>
/// Tudo é anulável porque a delta query devolve objetos parciais: uma mensagem removida vem só
/// com <c>id</c> e <c>@removed</c>, e campos fora do <c>$select</c> simplesmente não aparecem.
/// Exigir preenchimento derrubaria a página inteira por causa de uma linha — e a página inteira
/// é a varredura de uma caixa.
/// </remarks>
internal sealed record GraphErrorResponse([property: JsonPropertyName("error")] GraphError? Error);

internal sealed record GraphError(
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("message")] string? Message);

internal sealed record GraphMessagePage(
    [property: JsonPropertyName("value")] IReadOnlyList<GraphMessage>? Value,

    /// <summary>Próxima página desta varredura. Enquanto existir, não há cursor a guardar.</summary>
    [property: JsonPropertyName("@odata.nextLink")] string? NextLink,

    /// <summary>
    /// Só aparece na <strong>última</strong> página. É o cursor da próxima varredura — e é por
    /// isso que uma varredura interrompida no meio não pode avançar cursor nenhum.
    /// </summary>
    [property: JsonPropertyName("@odata.deltaLink")] string? DeltaLink);

internal sealed record GraphMessage(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("subject")] string? Subject,
    [property: JsonPropertyName("receivedDateTime")] DateTimeOffset? ReceivedDateTime,
    [property: JsonPropertyName("hasAttachments")] bool? HasAttachments,
    [property: JsonPropertyName("from")] GraphRecipient? From,

    /// <summary>
    /// Presente quando a mensagem foi apagada ou movida para fora da pasta desde o último
    /// cursor. Ignoramos: o que já foi ingerido é trilha de auditoria e não se desfaz porque
    /// alguém arrumou a caixa de entrada.
    /// </summary>
    [property: JsonPropertyName("@removed")] GraphRemoved? Removed);

internal sealed record GraphRemoved([property: JsonPropertyName("reason")] string? Reason);

internal sealed record GraphRecipient([property: JsonPropertyName("emailAddress")] GraphEmailAddress? EmailAddress);

internal sealed record GraphEmailAddress([property: JsonPropertyName("address")] string? Address);

internal sealed record GraphAttachmentPage(
    [property: JsonPropertyName("value")] IReadOnlyList<GraphAttachment>? Value);

internal sealed record GraphAttachment(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("contentType")] string? ContentType,
    [property: JsonPropertyName("size")] long? Size,

    /// <summary>
    /// Imagem embutida no corpo — logotipo, assinatura, ícone de rede social. Sempre descartada:
    /// é o que mais aparece numa caixa e nunca é boleto.
    /// </summary>
    [property: JsonPropertyName("isInline")] bool? IsInline);

internal sealed record GraphFolderPage(
    [property: JsonPropertyName("value")] IReadOnlyList<GraphFolder>? Value);

internal sealed record GraphFolder(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("displayName")] string? DisplayName);

/// <summary>Resposta do endpoint de token do Entra ID (client credentials).</summary>
internal sealed record GraphTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("expires_in")] int? ExpiresIn);
