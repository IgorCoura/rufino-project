namespace BillPayment.Infra.DocumentIntelligence.Gemini;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// O corpo da requisição de geração. Só a fatia que este BC usa.
/// </summary>
/// <remarks>
/// <strong>Sem SDK de terceiro, de propósito</strong> — a superfície usada é minúscula, e um
/// pacote do provedor no <c>.csproj</c> é exatamente o acoplamento que o ADR-013 existe para
/// evitar. Trocar de IA não deve exigir remover dependência de build.
/// </remarks>
internal sealed record GeminiRequest(
    [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
    [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

internal sealed record GeminiContent(
    [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

/// <summary>
/// Uma parte do conteúdo: ou texto, ou dado embutido. Nunca os dois.
/// </summary>
internal sealed record GeminiPart
{
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    [JsonPropertyName("inline_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiInlineData? InlineData { get; init; }

    public static GeminiPart FromText(string text) => new() { Text = text };

    public static GeminiPart FromDocument(string mediaType, ReadOnlyMemory<byte> content)
        => new() { InlineData = new GeminiInlineData(mediaType, Convert.ToBase64String(content.Span)) };
}

internal sealed record GeminiInlineData(
    [property: JsonPropertyName("mime_type")] string MimeType,
    [property: JsonPropertyName("data")] string Data);

/// <summary>
/// <c>responseSchema</c> + <c>responseMimeType</c> são o que tornam a resposta previsível.
/// </summary>
/// <remarks>
/// Sem structured output imposto pelo provedor, o ADR-011 perde uma perna: seria preciso extrair
/// JSON de texto livre, e erro de parsing viraria "não achei boleto" — indistinguível de um
/// documento que realmente não tem. <c>temperature 0</c> pelo mesmo motivo: a tarefa é ler número
/// impresso, não escrever.
/// </remarks>
internal sealed record GeminiGenerationConfig(
    [property: JsonPropertyName("responseMimeType")] string ResponseMimeType,
    [property: JsonPropertyName("responseSchema")] JsonElement ResponseSchema,
    [property: JsonPropertyName("temperature")] double Temperature);

internal sealed record GeminiResponse(
    [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates,
    [property: JsonPropertyName("usageMetadata")] GeminiUsage? UsageMetadata);

internal sealed record GeminiCandidate(
    [property: JsonPropertyName("content")] GeminiContent? Content,
    [property: JsonPropertyName("finishReason")] string? FinishReason);

/// <summary>Contagem de tokens, para a métrica de custo por documento extraído (doc 10).</summary>
internal sealed record GeminiUsage(
    [property: JsonPropertyName("promptTokenCount")] int? PromptTokenCount,
    [property: JsonPropertyName("candidatesTokenCount")] int? CandidatesTokenCount);

/// <summary>
/// A forma que o modelo é <em>obrigado</em> a devolver — e o mapa para <c>ExtractedDocument</c>.
/// </summary>
/// <remarks>
/// Todo campo é anulável e nenhum é confiável: isto é o que o modelo <strong>propôs</strong>. As
/// listas de linha e de Pix atravessam o DV e o CRC antes de virar instrumento; valor e
/// vencimento existem só para conferência cruzada contra a consulta oficial (ADR-011).
/// </remarks>
internal sealed record GeminiExtraction(
    [property: JsonPropertyName("digitableLines")] IReadOnlyList<string>? DigitableLines,
    [property: JsonPropertyName("pixPayloads")] IReadOnlyList<string>? PixPayloads,
    [property: JsonPropertyName("documentKind")] string? DocumentKind,
    [property: JsonPropertyName("payerName")] string? PayerName,
    [property: JsonPropertyName("payerTaxId")] string? PayerTaxId,
    [property: JsonPropertyName("payeeName")] string? PayeeName,
    [property: JsonPropertyName("payeeTaxId")] string? PayeeTaxId,
    [property: JsonPropertyName("accountReference")] string? AccountReference,
    [property: JsonPropertyName("amount")] string? Amount,
    [property: JsonPropertyName("dueDate")] string? DueDate,
    [property: JsonPropertyName("notes")] string? Notes);
